using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
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

    public async Task RunSocket()
    {
        var buf = new byte[512];
        var recv = await _ws.ReceiveAsync(buf, CancellationToken.None);
        while (!recv.CloseStatus.HasValue)
        {
            if (!_updateQueue.TryDequeue(out _))
            {
                await Task.Yield();
            }
            else
            {
                await _ws.SendAsync(buf, WebSocketMessageType.Binary, true, CancellationToken.None);
            }

            recv = await _ws.ReceiveAsync(buf, CancellationToken.None);
        }

        OnClose?.Invoke();

        await _ws.CloseAsync(
            WebSocketCloseStatus.Empty,
            "",
            CancellationToken.None);

        _cs.TrySetResult(true);
    }
}