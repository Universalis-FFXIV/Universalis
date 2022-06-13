using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Priority_Queue;
using Prometheus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public class SocketClient
{
    private const int QueueLimit = 30;

    private static readonly RecyclableMemoryStreamManager MemoryStreamPool = new();

    private readonly SimplePriorityQueue<SocketMessage, long> _messages;
    private readonly WebSocket _ws;
    private readonly TaskCompletionSource<object> _cs;
    private readonly ILogger _logger;
    private readonly object _runningLock;
    
    private readonly IList<EventCondition> _conditions;

    private SemaphoreSlim _recv;

    public Action OnClose { get; set; }
    public bool Running { get; private set; }

    private static readonly Histogram DiscardedMessages = Metrics.CreateHistogram("universalis_ws_discarded_messages", "WebSocket Discarded Messages");

    public SocketClient(WebSocket ws, TaskCompletionSource<object> cs, ILogger logger)
    {
        _messages = new SimplePriorityQueue<SocketMessage, long>();
        _runningLock = true;
        _conditions = new List<EventCondition>();

        _ws = ws;
        _cs = cs;
        _logger = logger;
    }

    public void Push(SocketMessage message)
    {
        // Check if this socket is expecting this kind of message. If the
        // client hasn't subscribed to any channels, this will not send
        // any messages.
        if (!_conditions.Any(cond => cond.ShouldSend(message)))
        {
            return;
        }

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
        if (_recv.CurrentCount != 0)
        {
            return;
        }
        
        try
        {
            _recv?.Release();
        }
        catch (ObjectDisposedException)
        {
            _logger.LogWarning("Semaphore is disposed");
        }
        catch (SemaphoreFullException)
        {
            _logger.LogWarning("Semaphore is full");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Semaphore release failed for an unknown reason");
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
            // Run the outbound and inbound data loops
            await Task.WhenAny(OutboundLoop(cancellationToken), InboundLoop(cancellationToken));

            if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
            {
                await _ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "closing socket",
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "WebSocket loop aborted with an exception");
        }
        finally
        {
            OnClose?.Invoke();
            _cs.TrySetResult(true);
            _recv.Dispose();
        }

        lock (_runningLock)
        {
            Running = false;
        }
    }

    private async Task OutboundLoop(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            // Wait for data to be made available
            await _recv.WaitAsync(cancellationToken);

            while (_messages.TryDequeue(out var message))
            {
                // So long as there's at least one await in this while loop,
                // it shouldn't block other threads.
                await SendEvent(message, cancellationToken);
            }
        }
    }

    private async Task InboundLoop(CancellationToken cancellationToken = default)
    {
        // Limit inbound message size to 1KB
        var buf = new byte[1024];

        while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            // Ideally we would only allocate the buffer as needed since inbound messages are
            // infrequent, but there doesn't seem to be a way of doing that without refactoring
            // the entire system into a one that loops over the connections, which probably
            // doesn't scale well for many connections.
            var res = await _ws.ReceiveAsync(buf, cancellationToken);
            if (res.CloseStatus != null)
            {
                break;
            }

            ReceiveEvent(buf);
        }
    }

    private void ReceiveEvent(byte[] buf)
    {
        BsonDocument data;
        try
        {
            data = BsonSerializer.Deserialize<BsonDocument>(buf);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "BSON deserialization failed");
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
                for (var i = 0; i < _conditions.Count; i++)
                {
                    if (_conditions[i].Equals(unsubCond))
                    {
                        _conditions.RemoveAt(i);
                        break;
                    }
                }

                break;
        }
    }

    private async Task SendEvent(SocketMessage message, CancellationToken cancellationToken = default)
    {
        await using var stream = MemoryStreamPool.GetStream() as RecyclableMemoryStream;
        if (stream == null)
        {
            return;
        }
        
        using var writer = new BsonBinaryWriter(stream);
        BsonSerializer.Serialize(writer, message.GetType(), message);

        var cur = 0;
        var end = (int)stream.Position;
        foreach (var memory in stream.GetReadOnlySequence())
        {
            if (cur + memory.Length >= end)
            {
                var lastIdx = end - cur;
                await _ws.SendAsync(memory[..lastIdx], WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, cancellationToken);
                break;
            }

            cur += memory.Length;
            await _ws.SendAsync(memory, WebSocketMessageType.Binary, WebSocketMessageFlags.None, cancellationToken);
        }
    }
}