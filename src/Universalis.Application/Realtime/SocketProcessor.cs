using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public class SocketProcessor : ISocketProcessor
{
    private readonly ConcurrentDictionary<Guid, SocketClient> _connections;

    private static readonly Gauge WebSocketConnections = Metrics.CreateGauge("universalis_ws_connections", "WebSocket Connections");
    private static readonly Counter MessagesSent = Metrics.CreateCounter("universalis_ws_sent", "WebSocket Messages Sent");

    public SocketProcessor()
    {
        _connections = new ConcurrentDictionary<Guid, SocketClient>();
    }

    public void BroadcastUpdate(SocketMessage message)
    {
        foreach (var (_, connection) in _connections)
        {
            connection.Push(message);
            MessagesSent.Inc();
        }
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