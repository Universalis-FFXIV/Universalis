using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.Application.Realtime;

public class SocketProcessor : ISocketProcessor
{
    private readonly ConcurrentDictionary<Guid, SocketClient> _connections;

    public SocketProcessor()
    {
        _connections = new ConcurrentDictionary<Guid, SocketClient>();
    }

    public void BroadcastUpdate(object o)
    {
        foreach (var (_, connection) in _connections)
        {
            connection.Push(o);
        }
    }

    public void AddSocket(WebSocket ws, TaskCompletionSource<object> cs, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();

        var conn = new SocketClient(ws, cs);
        conn.OnClose += () => _connections.TryRemove(id, out _);
        _ = conn.RunSocket(cancellationToken);

        _connections[id] = conn;
    }
}