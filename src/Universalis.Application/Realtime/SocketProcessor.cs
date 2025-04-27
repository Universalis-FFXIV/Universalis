using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public class SocketProcessor : ISocketProcessor, IDisposable
{
    private const int NumWorkers = 8;

    private static readonly Gauge WebSocketConnections = Metrics.CreateGauge(
        "universalis_ws_connections",
        "WebSocket Connections");

    private static readonly Histogram MessageQueueTime = Metrics.CreateHistogram(
        "universalis_ws_queue_milliseconds",
        "WebSocket Message Queue Milliseconds",
        new HistogramConfiguration
        {
            Buckets = [.005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10, 12.5, 15, 17.5, 20],
        });

    private static readonly Counter MessagesSent = Metrics.CreateCounter(
        "universalis_ws_sent",
        "WebSocket Messages Sent");
    private static readonly Counter WorkerExceptions = Metrics.CreateCounter(
        "universalis_ws_worker_exceptions",
        "WebSocket Worker Exceptions");

    private readonly ConcurrentDictionary<Guid, ISocketClient> _connections = new();
    private readonly BlockingCollection<Action> _taskQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<Thread> _workers = [];
    private readonly ILogger<SocketProcessor> _logger;

    public SocketProcessor(ILogger<SocketProcessor> logger)
    {
        _logger = logger;

        for (var i = 0; i < NumWorkers; i++)
        {
            CreateWorkerThread();
        }
    }

    public void Publish(SocketMessage message)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Prebuild the list of tasks to avoid a race condition between creating the
        // countdown event with a specific count and dispatching the tasks themselves.
        // Instead of using _connections.Count and then separately iterating over it,
        // We iterate the connections and then get the count of the resulting list.
        // We could use a lock here, but the fewer locks, the better.
        var newTasks = _connections
            .Select(kvp => kvp.Value)
            .Select(connection => CreatePushMessageTask(connection, message))
            .ToList();

        // Enqueue tasks for all connected clients
        using var countdownEvent = new CountdownEvent(newTasks.Count);
        foreach (var task in newTasks)
        {
            _taskQueue.Add(() =>
            {
                task();

                // ReSharper disable once AccessToDisposedClosure
                countdownEvent.Signal();
            });
        }

        // Wait until all clients have processed the message
        countdownEvent.Wait();

        stopwatch.Stop();
        MessageQueueTime.Observe(stopwatch.ElapsedMilliseconds);
    }

    public void AddSocket(WebSocket ws, TaskCompletionSource<object> cs, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();

        var conn = new SocketClient(ws, cs, new LoggerShield<SocketProcessor>(_logger, id));
        conn.OnClose += () =>
        {
            _connections.TryRemove(id, out _);
            WebSocketConnections.Dec();
        };

        _ = conn.RunSocket(cancellationToken);

        _connections[id] = conn;
        WebSocketConnections.Inc();
    }

    private void ProcessQueue()
    {
        var logger = new LoggerShield<SocketProcessor>(_logger, "Universalis SocketProcessor Worker");
        try
        {
            foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                try
                {
                    task();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing task");
                    WorkerExceptions.Inc();
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Queue worker cancelled");
        }
    }

    private static Action CreatePushMessageTask(ISocketClient connection, SocketMessage message)
    {
        return () =>
        {
            connection.Push(message);
            MessagesSent.Inc();
        };
    }

    private void CreateWorkerThread()
    {
        var thread = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = "Universalis SocketProcessor Worker",
        };
        _workers.Add(thread);
        thread.Start();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        // Mark the queue as completed
        _cancellationTokenSource.Cancel();
        _taskQueue.CompleteAdding();

        // Wait for all worker threads to complete
        foreach (var worker in _workers)
        {
            worker.Join();
        }

        _taskQueue.Dispose();
        _cancellationTokenSource.Dispose();
    }
}