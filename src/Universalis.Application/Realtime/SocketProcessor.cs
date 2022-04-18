using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public class SocketProcessor : ISocketProcessor
{
    private readonly ConcurrentDictionary<Guid, SocketClient> _connections;

    private static readonly Gauge WebSocketConnections = Metrics.CreateGauge("universalis_ws_connections", "WebSocket Connections");
    private static readonly Histogram MessageQueueTime = Metrics.CreateHistogram("universalis_ws_queue_milliseconds", "WebSocket Message Queue Milliseconds");
    private static readonly Counter MessagesSent = Metrics.CreateCounter("universalis_ws_sent", "WebSocket Messages Sent");

    public SocketProcessor()
    {
        _connections = new ConcurrentDictionary<Guid, SocketClient>();
    }

    public void Publish(SocketMessage message)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var (_, connection) in _connections)
        {
            connection.Push(message);
            MessagesSent.Inc();
        }

        stopwatch.Stop();
        MessageQueueTime.Observe(stopwatch.ElapsedMilliseconds);
    }

    public void AddSocket(WebSocket ws, TaskCompletionSource<object> cs, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();

        var conn = new SocketClient(ws, cs);
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