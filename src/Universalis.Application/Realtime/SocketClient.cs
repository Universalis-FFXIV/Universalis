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
    int capacity = SocketClient.DefaultCapacity)
    : ISocketClient
{
    /// <summary>
    /// Default outbound buffer capacity per client. Chosen to absorb roughly
    /// ~18 seconds of burst traffic at the steady-state per-client send rate
    /// currently (5/5/2026) observed in production of ~28msg/s. This hopefully
    /// avoids transient slow-client TCP backpressure silently dropping messages.
    /// </summary>
    public const int DefaultCapacity = 512;

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
        try
        {
            var outbound = OutboundLoop(cancellationToken);
            var inbound = InboundLoop(inboundCts.Token);

            // Wait for either loop to finish, then gracefully unwind the other:
            //  - if inbound finished first, complete the writer so outbound drains
            //    remaining buffered messages and exits cleanly;
            //  - if outbound finished first (typically an exception), cancel inbound.
            await Task.WhenAny(outbound, inbound);

            _channel.Writer.TryComplete();
            await inboundCts.CancelAsync();

            try
            {
                await Task.WhenAll(outbound, inbound);
            }
            catch
            {
                // Both loops swallow expected cancellations internally; anything that
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
