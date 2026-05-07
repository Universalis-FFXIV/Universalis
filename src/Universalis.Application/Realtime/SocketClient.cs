using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public class SocketClient(
    WebSocket ws,
    TaskCompletionSource<object> cs,
    ILogger logger,
    int capacity = SocketClient.DefaultCapacity,
    TimeSpan? stuckTimeout = null)
    : ISocketClient
{
    /// <summary>
    /// Default outbound buffer capacity per client. Chosen to absorb roughly
    /// ~18 seconds of burst traffic at the steady-state per-client send rate
    /// currently (5/5/2026) observed in production of ~28msg/s. This hopefully
    /// avoids transient slow-client TCP backpressure silently dropping messages.
    /// </summary>
    public const int DefaultCapacity = 512;

    /// <summary>
    /// Default sustained-stuck duration before the watchdog aborts the
    /// connection. Picked so a transient backpressure event does not
    /// produce a false-positive abort.
    /// </summary>
    public static readonly TimeSpan DefaultStuckTimeout = TimeSpan.FromSeconds(30);

    private readonly TimeSpan _stuckTimeout = stuckTimeout ?? DefaultStuckTimeout;

    private static readonly RecyclableMemoryStreamManager MemoryStreamPool = new();

    private readonly Channel<SocketMessage> _channel = Channel.CreateBounded<SocketMessage>(
        new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    private readonly object _runningLock = true;

    private readonly List<EventCondition> _conditions = [];

    /// <summary>0 = healthy; 1 = had a drop and not yet recovered.</summary>
    private long _slowClientFlag;

    /// <summary>
    /// Stopwatch timestamp captured at the 0->1 transition of <see cref="_slowClientFlag"/>.
    /// Read by the watchdog to compute how long the client has been stuck.
    /// </summary>
    private long _slowClientSinceTicks;

    public Action OnClose { get; set; }
    public bool Running { get; private set; }

    private static readonly Histogram DiscardedMessages =
        Metrics.CreateHistogram("universalis_ws_discarded_messages", "WebSocket Discarded Messages");

    /// <summary>
    /// Per-client outbound buffer depth observed on each enqueue. Lets us see
    /// how full client queues are running across the fleet without high-cardinality
    /// per-client labels.
    /// </summary>
    private static readonly Histogram QueueDepthMetric =
        Metrics.CreateHistogram(
            "universalis_ws_client_queue_depth",
            "Per-client outbound queue depth observed at enqueue time",
            new HistogramConfiguration
            {
                Buckets = [0, 1, 2, 4, 8, 16, 32, 64, 128, 192, 256, 320, 384, 448, 480, 504, 512],
            });

    /// <summary>
    /// Wall time of each ws.SendAsync call. If this gets slow then we have backpressure.
    /// </summary>
    private static readonly Histogram SendLatencyMetric =
        Metrics.CreateHistogram(
            "universalis_ws_send_latency_milliseconds",
            "Per-message WebSocket.SendAsync latency in milliseconds",
            new HistogramConfiguration
            {
                Buckets = [.1, .25, .5, 1, 2.5, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000],
            });

    /// <summary>
    /// Counts WebSocket connections aborted by the watchdog because the
    /// outbound queue stayed saturated past <see cref="DefaultStuckTimeout"/>.
    /// Used to monitor false-positive rate of the abort path.
    /// </summary>
    private static readonly Counter ConnectionsAbortedBackpressure =
        Metrics.CreateCounter(
            "universalis_ws_connections_aborted_backpressure",
            "WebSocket connections aborted due to sustained queue saturation");

    /// <summary>Current outbound buffer depth. Exposed for tests and metrics inspection.</summary>
    internal int QueueDepth => _channel.Reader.Count;

    /// <summary>Configured outbound buffer capacity.</summary>
    internal int Capacity => capacity;

    /// <summary>
    /// Test-only hook: drain a single message without running the WebSocket loop.
    /// Behaves like <see cref="ChannelReader{T}.TryRead"/>.
    /// </summary>
    internal bool TryDequeueForTesting(out SocketMessage message) =>
        _channel.Reader.TryRead(out message);

    public void Push(SocketMessage message)
    {
        // Check if this socket is expecting this kind of message. If the
        // client hasn't subscribed to any channels, this will not enqueue
        // any messages.
        if (!_conditions.Any(cond => cond.ShouldSend(message)))
        {
            return;
        }

        // BoundedChannelFullMode.DropOldest evicts the oldest item when the
        // buffer is already at capacity. We detect this with a snapshot read
        // immediately before the write - racy with other concurrent producers
        // under heavy contention, but the worst case is small over/under-count
        // in the discard metric, which is acceptable for a monitoring signal.
        var willDrop = _channel.Reader.Count >= capacity;

        _channel.Writer.TryWrite(message);
        QueueDepthMetric.Observe(_channel.Reader.Count);

        if (willDrop)
        {
            DiscardedMessages.Observe(1);

            // Log only the first drop after a recovery so a sustained slow client
            // doesn't spam logs once per dropped message.
            if (Interlocked.Exchange(ref _slowClientFlag, 1) == 0)
            {
                Volatile.Write(ref _slowClientSinceTicks, Stopwatch.GetTimestamp());
                logger.LogWarning(
                    "WebSocket client queue overflowed (capacity={Capacity}); dropping oldest messages",
                    capacity);
            }
        }
    }

    /// <summary>
    /// Runs the WebSocket loop.
    /// </summary>
    public async Task RunSocket(CancellationToken cancellationToken = default)
    {
        lock (_runningLock)
        {
            if (Running)
            {
                throw new InvalidOperationException("The WebSocket loop is already running.");
            }

            Running = true;
        }

        using var inboundCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            var outbound = OutboundLoop(cancellationToken);
            var inbound = InboundLoop(inboundCts.Token);
            var watchdog = WatchdogLoop(watchdogCts.Token);

            // Wait for any of the three to finish, then gracefully unwind the others:
            //  - if inbound finished first, complete the writer so outbound drains
            //    remaining buffered messages and exits cleanly;
            //  - if outbound finished first (typically an exception), cancel inbound;
            //  - if watchdog finished, it has already aborted ws which will fault
            //    the pending SendAsync inside outbound.
            await Task.WhenAny(outbound, inbound, watchdog);

            _channel.Writer.TryComplete();
            await inboundCts.CancelAsync();
            await watchdogCts.CancelAsync();

            try
            {
                await Task.WhenAll(outbound, inbound, watchdog);
            }
            catch
            {
                // The loops swallow expected cancellations internally; anything that
                // bubbles here we ignore so it doesn't shadow the close path below.
            }

            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
            {
                await ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "closing socket",
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "WebSocket loop aborted with an exception");
        }
        finally
        {
            // ensure the writer completed even if we threw before the explicit completion above
            _channel.Writer.TryComplete();
            OnClose?.Invoke();
            cs.TrySetResult(true);
        }

        lock (_runningLock)
        {
            Running = false;
        }
    }

    private async Task OutboundLoop(CancellationToken cancellationToken)
    {
        var reader = _channel.Reader;
        try
        {
            await foreach (var message in reader.ReadAllAsync(cancellationToken))
            {
                if (ws.State != WebSocketState.Open)
                {
                    break;
                }

                var sw = Stopwatch.StartNew();
                try
                {
                    await SendEvent(message, cancellationToken);
                }
                finally
                {
                    sw.Stop();
                    SendLatencyMetric.Observe(sw.Elapsed.TotalMilliseconds);
                }

                // queue fully drained; client caught up.
                if (reader.Count == 0 && Interlocked.Exchange(ref _slowClientFlag, 0) == 1)
                {
                    logger.LogInformation("WebSocket client queue recovered");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    /// <summary>
    /// Watchdog loop. While the slow-client flag is set we measure elapsed time
    /// since it flipped; if the flag stays set for at least
    /// <see cref="_stuckTimeout"/> without recovering, the underlying WebSocket
    /// is aborted so the (likely deadlocked) <see cref="SendEvent"/> faults and
    /// the connection slot is freed for a healthy reconnect.
    /// </summary>
    private async Task WatchdogLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Volatile.Read(ref _slowClientFlag) == 0)
                {
                    // Healthy — sleep one full timeout, then re-check.
                    await Task.Delay(_stuckTimeout, cancellationToken);
                    continue;
                }

                // Slow flag is set — sleep precisely until the threshold elapses
                // since the flip.
                var since = Volatile.Read(ref _slowClientSinceTicks);
                var elapsed = Stopwatch.GetElapsedTime(since);
                var remaining = _stuckTimeout - elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, cancellationToken);
                    continue;
                }

                // Threshold exceeded. Re-check the flag once more in case the
                // client recovered between our last read and now;
                // we never want to abort a recovering client.
                if (Volatile.Read(ref _slowClientFlag) != 1)
                {
                    continue;
                }

                ConnectionsAbortedBackpressure.Inc();
                logger.LogWarning(
                    "Aborting WebSocket: outbound queue stuck near capacity for >= {Threshold}",
                    _stuckTimeout);

                ws.Abort();
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    private async Task InboundLoop(CancellationToken cancellationToken)
    {
        // Limit inbound message size to 1KB
        var buf = new byte[1024];

        while (!cancellationToken.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            // Ideally we would only allocate the buffer as needed since inbound messages are
            // infrequent, but there doesn't seem to be a way of doing that without refactoring
            // the entire system into a one that loops over the connections, which probably
            // doesn't scale well for many connections.
            var res = await ws.ReceiveAsync(buf, cancellationToken);
            if (res.CloseStatus != null)
            {
                break;
            }

            await ReceiveEvent(buf, cancellationToken);
        }
    }

    private async Task ReceiveEvent(byte[] buf, CancellationToken cancellationToken = default)
    {
        BsonDocument data;
        try
        {
            data = BsonSerializer.Deserialize<BsonDocument>(buf);
        }
        catch (Exception e)
        {
            logger.LogError(e, "BSON deserialization failed");
            return;
        }

        string @event;
        try
        {
            @event = data["event"].AsString;
        }
        catch (InvalidCastException)
        {
            return;
        }

        var eventName = @event.ToLowerInvariant();
        switch (eventName)
        {
            case "subscribe":
                string subChannel;
                try
                {
                    subChannel = data["channel"].AsString;
                }
                catch (InvalidCastException)
                {
                    return;
                }

                var subCond = EventCondition.Parse(subChannel);
                var shouldAdd = true;
                for (var i = 0; i < _conditions.Count; i++)
                {
                    if (_conditions[i].Equals(subCond))
                    {
                        shouldAdd = false;
                        break;
                    }

                    // Replace the existing condition if the new condition is either more-specific or less-specific
                    // than the existing one. If the existing and new conditions are not related, do nothing here. 
                    if (_conditions[i].IsReplaceableWith(subCond) || subCond.IsReplaceableWith(_conditions[i]))
                    {
                        shouldAdd = false;
                        _conditions[i] = subCond;
                        break;
                    }
                }

                if (shouldAdd)
                {
                    _conditions.Add(subCond);
                }

                break;
            case "unsubscribe":
                string unsubChannel;
                try
                {
                    unsubChannel = data["channel"].AsString;
                }
                catch (InvalidCastException)
                {
                    return;
                }

                var unsubCond = EventCondition.Parse(unsubChannel);
                var conditionsCount = _conditions.Count;
                for (var i = 0; i < conditionsCount; i++)
                {
                    if (_conditions[i].IsReplaceableWith(unsubCond))
                    {
                        _conditions.RemoveAt(i);
                        conditionsCount--;
                    }
                }

                break;
            default:
                await SendEvent(new SubscribeFailure("Unknown client event"), cancellationToken);
                break;
        }
    }

    private async Task SendEvent(SocketMessage message, CancellationToken cancellationToken = default)
    {
        var bytes = message.GetSerializedBytes(MemoryStreamPool);
        await ws.SendAsync(bytes, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        GC.SuppressFinalize(this);
    }
}
