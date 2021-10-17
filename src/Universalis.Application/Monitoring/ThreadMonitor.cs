using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Universalis.Application.Monitoring
{
    public class ThreadPoolMonitor : IDisposable
    {
        private readonly ILogger<ThreadPoolMonitor> _logger;

        private Thread _monitorThread;
        private bool _active;

        public ThreadPoolMonitor(ILogger<ThreadPoolMonitor> logger)
        {
            _logger = logger;
        }

        public void Start()
        {
            _active = true;
            _monitorThread = new Thread(MonitoringLoop);
            _monitorThread.Start();
        }

        private void MonitoringLoop()
        {
            while (_active)
            {
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
                var (workerThreads, completionPortThreads) = GetActiveThreads();

                _logger.LogInformation("ThreadPool information:\n" +
                                       "Worker threads:\t\t\t{WorkerThreads}\n" +
                                       "Completion port threads:\t{CompletionPortThreads}\n" +
                                       "Max worker threads:\t\t{MaxWorkerThreads}\n" +
                                       "Max completion port threads:\t{MaxCompletionPortThreads}",
                    workerThreads, completionPortThreads, maxWorkerThreads, maxCompletionPortThreads);

                Thread.Sleep(new TimeSpan(0, 0, 10));
            }
        }

        /// <summary>
        /// Returns the number of active threads of each type owned by the default <see cref="ThreadPool"/>.
        /// </summary>
        private static ActiveThreadsInfo GetActiveThreads()
        {
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
            return new ActiveThreadsInfo(
                maxWorkerThreads - availableWorkerThreads,
                maxCompletionPortThreads - availableCompletionPortThreads);
        }

        private record ActiveThreadsInfo(int WorkerThreads, int CompletionPortThreads);

        public void Dispose()
        {
            _active = false;

            try
            {
                _monitorThread.Interrupt();
                var joined = _monitorThread?.Join(new TimeSpan(0, 0, 5));
                if (joined == false)
                {
                    _logger.LogWarning("Failed to join ThreadPool monitoring thread.");
                }
            }
            catch (ThreadStateException e)
            {
                _logger.LogWarning(e, "Exception occurred when shutting down ThreadPool monitor.");
            }

            GC.SuppressFinalize(this);
        }
    }
}