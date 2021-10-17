using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Universalis.Application.Monitoring
{
    public class ThreadPoolMonitorOptions
    {
        public string Filename { get; set; }

        public long MaxSizeBytes { get; set; }
    }

    public class ThreadPoolMonitor : IDisposable
    {
        private readonly ILogger<ThreadPoolMonitor> _logger;
        private readonly MonitoringLog<ThreadPoolInfo, ThreadPoolInfoMap> _log;

        private Thread _monitorThread;
        private bool _active;

        public ThreadPoolMonitor(ILogger<ThreadPoolMonitor> logger, IOptions<ThreadPoolMonitorOptions> options)
        {
            var opts = options.Value;

            _logger = logger;
            _log = new MonitoringLog<ThreadPoolInfo, ThreadPoolInfoMap>(opts.Filename, opts.MaxSizeBytes);
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

                _logger.LogInformation("ThreadPool information ({Time}):\n" +
                                       "Worker threads:\t\t\t{WorkerThreads}\n" +
                                       "Completion port threads:\t\t{CompletionPortThreads}\n" +
                                       "Max worker threads:\t\t{MaxWorkerThreads}\n" +
                                       "Max completion port threads:\t{MaxCompletionPortThreads}",
                    DateTimeOffset.UtcNow, workerThreads, completionPortThreads, maxWorkerThreads, maxCompletionPortThreads);

                try
                {
                    _log.Append(new ThreadPoolInfo
                    {
                        UnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        WorkerThreads = workerThreads,
                        CompletionPortThreads = completionPortThreads,
                        MaxWorkerThreads = maxWorkerThreads,
                        MaxCompletionPortThreads = maxCompletionPortThreads,
                    });
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception while writing log line.");
                }

                Thread.Sleep(new TimeSpan(0, 1, 0));
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