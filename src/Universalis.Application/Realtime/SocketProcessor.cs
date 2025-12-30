using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public class SocketProcessor(ILogger<SocketProcessor> logger) : ISocketProcessor
{
    private static readonly Gauge WebSocketConnections = Metrics.CreateGauge(
        "universalis_ws_connections",
        "WebSocket Connections");

    private static readonly Histogram MessageQueueTime = Metrics.CreateHistogram(
        "universalis_ws_queue_milliseconds",
        "WebSocket Message Queue Milliseconds",
        new HistogramConfiguration
        {
            Buckets = [.005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10, 12.5, 15, 17.5, 20],
        });

    private static readonly Counter MessagesSent = Metrics.CreateCounter(
        "universalis_ws_sent",
        "WebSocket Messages Sent");
    private static readonly Counter ExceptionCount = Metrics.CreateCounter(
        "universalis_ws_exceptions",
        "WebSocket exceptions across all connections");

    private static readonly RecyclableMemoryStreamManager MemoryStreamPool = new();

    private readonly ConcurrentDictionary<Guid, ISocketClient> _connections = new();

    private static byte[] SerializeMessage(SocketMessage message)
    {
        using var stream = MemoryStreamPool.GetStream();
        using var writer = new BsonBinaryWriter(stream);
        BsonSerializer.Serialize(writer, message.GetType(), message);
        return stream.ToArray();
    }

    public void Publish(SocketMessage message)
    {
        var stopwatch = Stopwatch.StartNew();

        // Serialize once and cache on the message
        message.CachedSerializedBytes = SerializeMessage(message);

        Parallel.ForEach(
            _connections,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
            kvp =>
            {
                try
                {
                    kvp.Value.Push(message);
                    MessagesSent.Inc();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send message to connection {}", kvp.Key);
                    ExceptionCount.Inc();
                }
            });

        stopwatch.Stop();
        MessageQueueTime.Observe(stopwatch.ElapsedMilliseconds);
    }

    public void AddSocket(WebSocket ws, TaskCompletionSource<object> cs, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();

        var conn = new SocketClient(ws, cs, new LoggerShield<SocketProcessor>(logger, id));
        conn.OnClose += () =>
        {
            _connections.TryRemove(id, out _);
            WebSocketConnections.Dec();
        };

        _ = conn.RunSocket(cancellationToken);

        _connections[id] = conn;
        WebSocketConnections.Inc();
    }
}