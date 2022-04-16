using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.Application.Realtime;

public class SocketClient
{
    private readonly ConcurrentQueue<object> _updateQueue;
    private readonly WebSocket _ws;
    private readonly TaskCompletionSource<object> _cs;

    public Action OnClose { get; set; }

    public SocketClient(WebSocket ws, TaskCompletionSource<object> cs)
    {
        _updateQueue = new ConcurrentQueue<object>();

        _ws = ws;
        _cs = cs;
    }

    public void Push(object o)
    {
        _updateQueue.Enqueue(o);
        while (_updateQueue.Count > 20)
        {
            _updateQueue.TryDequeue(out _);
        }
    }

    public async Task RunSocket(CancellationToken cancellationToken = default)
    {
        var buf = new byte[512];
        while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            if (!_updateQueue.TryDequeue(out _))
            {
                var n = Encoding.UTF8.GetBytes("{\"status\": \"ok\"}", buf);
                await _ws.SendAsync(buf.AsMemory(..n), WebSocketMessageType.Text, WebSocketMessageFlags.None, CancellationToken.None);
                // await Task.Yield();
            }
            else
            {
                await _ws.SendAsync(buf, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        OnClose?.Invoke();

        await _ws.CloseAsync(
            WebSocketCloseStatus.Empty,
            "",
            CancellationToken.None);

        _cs.TrySetResult(true);
    }
}