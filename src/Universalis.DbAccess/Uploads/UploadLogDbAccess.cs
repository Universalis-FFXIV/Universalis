using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Universalis.Entities.Uploads;
using PrometheusMetrics = Prometheus.Metrics;

namespace Universalis.DbAccess.Uploads;

/// <summary>
/// Queues upload log entries through a bounded channel and flushes them to the
/// underlying <see cref="IUploadLogStore"/> in batches from a background loop.
///
/// The earlier inline-per-upload implementation was disabled in PR #1302 because
/// each upload triggered 1-3 awaited INSERTs on the request path. This wrapper
/// keeps the request path free of database I/O at the cost of best-effort durability:
/// when the channel saturates the oldest queued entries are dropped, and an INSERT
/// failure drops the entire current batch.
/// </summary>
public class UploadLogDbAccess : IUploadLogDbAccess, IHostedService
{
    public const int DefaultCapacity = 50_000;
    public const int DefaultMaxBatchSize = 500;

    private static readonly Histogram QueueDepthMetric =
        PrometheusMetrics.CreateHistogram(
            "universalis_upload_log_queue_depth",
            "Upload log channel depth observed at enqueue time",
            new HistogramConfiguration
            {
                Buckets = [0, 1, 10, 100, 500, 1_000, 5_000, 10_000, 25_000, 50_000],
            });

    private static readonly Counter DroppedEntriesMetric =
        PrometheusMetrics.CreateCounter(
            "universalis_upload_log_dropped_entries",
            "Upload log entries discarded because the channel was at capacity");

    private static readonly Counter FlushFailuresMetric =
        PrometheusMetrics.CreateCounter(
            "universalis_upload_log_flush_failures",
            "Upload log batch flushes that threw");

    private static readonly Histogram BatchSizeMetric =
        PrometheusMetrics.CreateHistogram(
            "universalis_upload_log_batch_size",
            "Size of upload log batches as they are flushed to the store",
            new HistogramConfiguration
            {
                Buckets = [1, 5, 10, 25, 50, 100, 250, 500],
            });

    private readonly IUploadLogStore _store;
    private readonly ILogger<UploadLogDbAccess> _logger;
    private readonly Channel<UploadLogEntry> _channel;
    private readonly int _capacity;
    private readonly int _maxBatchSize;

    private Task _drainTask = Task.CompletedTask;

    public UploadLogDbAccess(
        IUploadLogStore store,
        ILogger<UploadLogDbAccess> logger,
        int capacity = DefaultCapacity,
        int maxBatchSize = DefaultMaxBatchSize)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "capacity must be positive");
        }

        if (maxBatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBatchSize), maxBatchSize, "maxBatchSize must be positive");
        }

        _store = store;
        _logger = logger;
        _capacity = capacity;
        _maxBatchSize = maxBatchSize;
        _channel = Channel.CreateBounded<UploadLogEntry>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public Task LogAction(UploadLogEntry entry)
    {
        // DropOldest evicts the oldest item when the buffer is at capacity. The
        // snapshot read here is racy with other producers under heavy contention,
        // but the worst case is a small over/under-count on the discard metric.
        var willDrop = _channel.Reader.Count >= _capacity;

        _channel.Writer.TryWrite(entry);
        QueueDepthMetric.Observe(_channel.Reader.Count);

        if (willDrop)
        {
            DroppedEntriesMetric.Inc();
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _drainTask = Task.Run(DrainLoopAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Completing the writer lets the drain loop finish naturally once the
        // channel is empty, so any queued entries get flushed before shutdown.
        _channel.Writer.TryComplete();

        try
        {
            await _drainTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Upload log batcher did not finish draining before shutdown deadline; queued entries lost");
        }
    }

    private async Task DrainLoopAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync())
            {
                await DrainOnceAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload log batcher drain loop terminated unexpectedly");
        }
    }

    internal async Task DrainOnceAsync(CancellationToken cancellationToken = default)
    {
        var batch = new List<UploadLogEntry>(_maxBatchSize);
        while (batch.Count < _maxBatchSize && _channel.Reader.TryRead(out var entry))
        {
            batch.Add(entry);
        }

        if (batch.Count == 0)
        {
            return;
        }

        BatchSizeMetric.Observe(batch.Count);

        try
        {
            await _store.LogActions(batch, cancellationToken);
        }
        catch (Exception ex)
        {
            FlushFailuresMetric.Inc();
            _logger.LogError(ex, "Failed to flush upload log batch of size {BatchSize}", batch.Count);
        }
    }
}
