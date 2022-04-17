using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Message;

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
                await SendEvent(buf, new ItemUpdate { ItemId = 2001 }, cancellationToken);
                // await Task.Yield();
            }
            else
            {
                await _ws.SendAsync(buf, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, cancellationToken);
            }
        }

        OnClose?.Invoke();

        await _ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "closing socket",
            cancellationToken);

        _cs.TrySetResult(true);
    }

    private async Task SendEvent(byte[] buf, SocketMessage message, CancellationToken cancellationToken = default)
    {
        await using var streamView = new MemoryStream(buf, true);
        await JsonSerializer.SerializeAsync(streamView, (object)message, cancellationToken: cancellationToken);

        var pos = (int)streamView.Position;
        await _ws.SendAsync(buf.AsMemory(..pos), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }
}