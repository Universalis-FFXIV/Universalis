using Priority_Queue;
using Prometheus;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public class SocketClient
{
    private const int QueueLimit = 20;

    private readonly SimplePriorityQueue<SocketMessage, long> _messages;
    private readonly WebSocket _ws;
    private readonly TaskCompletionSource<object> _cs;
    private readonly object _runningLock;

    private SemaphoreSlim _recv;

    public Action OnClose { get; set; }
    public bool Running { get; private set; }

    private static readonly Histogram DiscardedMessages = Metrics.CreateHistogram("universalis_ws_discarded_messages", "WebSocket Discarded Messages");

    public SocketClient(WebSocket ws, TaskCompletionSource<object> cs)
    {
        _messages = new SimplePriorityQueue<SocketMessage, long>();
        _runningLock = true;

        _ws = ws;
        _cs = cs;
    }

    public void Push(SocketMessage message)
    {
        _messages.Enqueue(message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        // We keep an incrementing count of discarded messages because
        // the consumer can still pull messages off while we're removing
        // them.
        var discarded = 0;
        while (_messages.Count > QueueLimit)
        {
            // We don't want backlog to create memory issues, but this shouldn't happen
            // on most connections anyways.
            if (_messages.TryDequeue(out _))
            {
                discarded++;
            }
        }

        if (discarded > 0)
        {
            DiscardedMessages.Observe(discarded);
        }

        // Release the semaphore, if applicable
        if (_recv.CurrentCount == 0)
        {
            try
            {
                _recv?.Release();
            }
            catch (ObjectDisposedException)
            {
                // ignored
            }
            catch (SemaphoreFullException)
            {
                // ignored
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

        // Create a blocked semaphore with one consumer
        _recv = new SemaphoreSlim(0, 1);

        try
        {
            var buf = new byte[512]; // TODO: something
            while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
            {
                // Wait for data to be made available
                await _recv.WaitAsync(cancellationToken);

                while (_messages.TryDequeue(out var message))
                {
                    // So long as there's at least one await in this while loop,
                    // it shouldn't block other threads.
                    await SendEvent(buf, message, cancellationToken);
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

        _recv.Dispose();

        lock (_runningLock)
        {
            Running = false;
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