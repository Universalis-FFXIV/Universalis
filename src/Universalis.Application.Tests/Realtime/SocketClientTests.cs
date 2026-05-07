using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prometheus;
using Universalis.Application.Realtime;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Tests.Mocks.Realtime.Messages;
using Xunit;

namespace Universalis.Application.Tests.Realtime;

public class SocketClientTests
{
    private static SocketClient NewClient(int capacity, ILogger logger = null)
    {
        var ws = new Mock<WebSocket>();
        ws.SetupGet(w => w.State).Returns(WebSocketState.Open);
        var cs = new TaskCompletionSource<object>();
        return new SocketClient(ws.Object, cs, logger ?? NullLogger.Instance, capacity);
    }

    private static Histogram GetStaticHistogram(string fieldName)
    {
        var field = typeof(SocketClient).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return (Histogram)field!.GetValue(null)!;
    }

    private static void SubscribeToAll(SocketClient client, string channel = "test")
    {
        // Inject a condition by reflection so Push() doesn't no-op.
        var conditionsField = typeof(SocketClient).GetField(
            "_conditions",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(conditionsField);
        var list = (List<EventCondition>)conditionsField!.GetValue(client)!;
        list.Add(EventCondition.Parse(channel));
    }

    [Fact]
    public void Push_MatchingMessage_AddsToQueue()
    {
        var client = NewClient(capacity: 4);
        SubscribeToAll(client);

        client.Push(new MockMessage("test") { Value = 1 });

        Assert.Equal(1, client.QueueDepth);
    }

    [Fact]
    public void Push_NoMatchingSubscription_DoesNotAddToQueue()
    {
        // Sanity check: existing condition-filter behavior must still work.
        var client = NewClient(capacity: 4);
        SubscribeToAll(client, channel: "other");

        client.Push(new MockMessage("test") { Value = 1 });

        Assert.Equal(0, client.QueueDepth);
    }

    [Fact]
    public void Push_AboveCapacity_DropsOldestPreservingNewest()
    {
        // when a slow consumer can't keep up and the queue fills, we keep
        // the freshest messages and drop the staler ones.
        const int capacity = 4;
        var client = NewClient(capacity);
        SubscribeToAll(client);

        for (var i = 0; i < capacity + 3; i++)
        {
            client.Push(new MockMessage("test") { Value = i });
        }

        Assert.Equal(capacity, client.QueueDepth);

        var surviving = new List<int>();
        while (client.TryDequeueForTesting(out var msg))
        {
            surviving.Add(((MockMessage)msg).Value);
        }

        // Pushed values 0..6, capacity 4: oldest three (0,1,2) should be dropped,
        // and the surviving messages should still be in FIFO order.
        Assert.Equal([3, 4, 5, 6], surviving);
    }

    [Fact]
    public void Push_RecordsQueueDepthInPerClientMetric()
    {
        // every successful enqueue must observe the resulting depth
        // so we can see queue-fullness distributions in production
        // without high-cardinality per-client labels.
        var metric = GetStaticHistogram("QueueDepthMetric");
        var beforeCount = metric.Count;

        var client = NewClient(capacity: 8);
        SubscribeToAll(client);

        client.Push(new MockMessage("test") { Value = 1 });
        client.Push(new MockMessage("test") { Value = 2 });
        client.Push(new MockMessage("test") { Value = 3 });

        Assert.Equal(beforeCount + 3, metric.Count);
    }

    [Fact]
    public async Task OutboundLoop_RecordsSendLatencyPerMessage()
    {
        // each ws.SendAsync must be timed and observed so a right-shift
        // in the distribution flags slow clients before drops happen.
        var metric = GetStaticHistogram("SendLatencyMetric");
        var beforeCount = metric.Count;

        var fakeWs = new FakeWebSocket();
        var cs = new TaskCompletionSource<object>();
        var client = new SocketClient(fakeWs, cs, NullLogger.Instance, capacity: 4);
        SubscribeToAll(client);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var runTask = client.RunSocket(cts.Token);

        client.Push(new MockMessage("test") { Value = 1 });
        client.Push(new MockMessage("test") { Value = 2 });

        await fakeWs.WaitForSendsAsync(2, TimeSpan.FromSeconds(2));

        Assert.True(
            metric.Count >= beforeCount + 2,
            $"expected at least 2 send-latency observations; got delta {metric.Count - beforeCount}");

        cts.Cancel();
        try { await runTask; } catch { /* expected */ }
    }

    [Fact]
    public void Push_AboveCapacity_LogsSlowClientWarningOnce()
    {
        // the first drop after recovery should emit a warning so
        // we can identify slow clients.
        var loggerMock = new Mock<ILogger>();
        var client = NewClient(capacity: 4, logger: loggerMock.Object);
        SubscribeToAll(client);

        // Push capacity+3 -> 3 drop events fire OnItemDropped, but only the
        // first crossing should log a warning.
        for (var i = 0; i < 7; i++)
        {
            client.Push(new MockMessage("test") { Value = i });
        }

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Watchdog_QueueStuckBeyondTimeout_AbortsConnection()
    {
        // When SendAsync is parked on TCP backpressure, the per-client
        // outbound queue saturates and the slow-client flag stays set.
        // The watchdog flag holds for >= stuckTimeout and we call ws.Abort()
        // so the pending send unwinds and the connection slot is freed.
        var fakeWs = new FakeWebSocket { BlockSends = true };
        var cs = new TaskCompletionSource<object>();
        var stuckTimeout = TimeSpan.FromMilliseconds(200);
        var client = new SocketClient(
            fakeWs,
            cs,
            NullLogger.Instance,
            capacity: 4,
            stuckTimeout: stuckTimeout);
        SubscribeToAll(client);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var runTask = client.RunSocket(cts.Token);

        // Overflow capacity so _slowClientFlag flips to 1. With BlockSends
        // the OutboundLoop is parked on the first send and cannot drain.
        for (var i = 0; i < 10; i++)
        {
            client.Push(new MockMessage("test") { Value = i });
        }

        // Watchdog should fire within stuckTimeout + scheduling grace.
        await fakeWs.WaitForAbortAsync(TimeSpan.FromSeconds(3));

        Assert.Equal(WebSocketState.Aborted, fakeWs.State);

        await cts.CancelAsync();
        try { await runTask; } catch { /* expected */ }
    }

    private sealed class FakeWebSocket : WebSocket
    {
        private readonly List<byte[]> _sent = new();
        private readonly object _sentLock = new();
        private WebSocketState _state = WebSocketState.Open;

        // Fires when SendAsync is called with BlockSends = true. Faulted by
        // Abort() to simulate the underlying socket teardown causing the
        // pending send to throw WebSocketException.
        private readonly TaskCompletionSource _blockedSendTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Fires the first time Abort() is called.
        private readonly TaskCompletionSource _abortTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// When true, SendAsync parks until Abort() is called or the
        /// supplied cancellationToken fires. Simulates a peer that has stopped
        /// acking and stalls the kernel send buffer indefinitely.
        /// </summary>
        public bool BlockSends { get; init; }

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string SubProtocol => null;

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
            _blockedSendTcs.TrySetException(
                new WebSocketException(WebSocketError.ConnectionClosedPrematurely));
            _abortTcs.TrySetResult();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override void Dispose() { }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("unreachable");
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (BlockSends)
            {
                await using var registration = cancellationToken.Register(
                    () => _blockedSendTcs.TrySetCanceled(cancellationToken));
                await _blockedSendTcs.Task;
                return;
            }

            var copy = new byte[buffer.Count];
            Buffer.BlockCopy(buffer.Array!, buffer.Offset, copy, 0, buffer.Count);
            lock (_sentLock)
            {
                _sent.Add(copy);
            }
        }

        public async Task WaitForSendsAsync(int n, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                lock (_sentLock)
                {
                    if (_sent.Count >= n) return;
                }
                await Task.Delay(10);
            }
            throw new TimeoutException($"expected {n} sends within {timeout}; only saw {_sent.Count}");
        }

        public async Task WaitForAbortAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_abortTcs.Task, Task.Delay(timeout));
            if (completed != _abortTcs.Task)
            {
                throw new TimeoutException($"Abort() not called within {timeout}");
            }
        }
    }
}
