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
    private readonly ConcurrentQueue<SocketMessage> _messages;
    private readonly WebSocket _ws;
    private readonly TaskCompletionSource<object> _cs;

    public Action OnClose { get; set; }

    public SocketClient(WebSocket ws, TaskCompletionSource<object> cs)
    {
        _messages = new ConcurrentQueue<SocketMessage>();

        _ws = ws;
        _cs = cs;
    }

    public void Push(SocketMessage message)
    {
        _messages.Enqueue(message);
        while (_messages.Count > 20)
        {
            // We don't want backlog to create memory issues, but this shouldn't happen
            // on most connections anyways.
            _messages.TryDequeue(out _);
        }
    }

    public async Task RunSocket(CancellationToken cancellationToken = default)
    {
        try
        {
            var buf = new byte[512];
            while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
            {
                if (_messages.TryDequeue(out var message))
                {
                    await SendEvent(buf, message, cancellationToken);
                }
                else
                {
                    await Task.Yield();
                }
            }

            await _ws.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "closing socket",
                cancellationToken);
        }
        finally
        {
            OnClose?.Invoke();
            _cs.TrySetResult(true);
        }
    }

    private async Task SendEvent(byte[] buf, SocketMessage message, CancellationToken cancellationToken = default)
    {
        await using var streamView = new MemoryStream(buf, true);
        await JsonSerializer.SerializeAsync(streamView, (object)message, cancellationToken: cancellationToken);

        var pos = (int)streamView.Position;
        await _ws.SendAsync(buf.AsMemory(..pos), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }
}