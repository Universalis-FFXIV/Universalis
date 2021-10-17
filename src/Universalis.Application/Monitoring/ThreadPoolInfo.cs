using CsvHelper.Configuration;

namespace Universalis.Application.Monitoring
{
    public class ThreadPoolInfo
    {
        public long UnixMs { get; init; }

        public int WorkerThreads { get; init; }

        public int CompletionPortThreads { get; init; }

        public int MaxWorkerThreads { get; init; }

        public int MaxCompletionPortThreads { get; init; }
    }

    public sealed class ThreadPoolInfoMap : ClassMap<ThreadPoolInfo>
    {
        public ThreadPoolInfoMap()
        {
            Map(m => m.UnixMs).Index(0).Name("unix_ms");
            Map(m => m.WorkerThreads).Index(1).Name("worker_threads");
            Map(m => m.CompletionPortThreads).Index(2).Name("completion_port_threads");
            Map(m => m.MaxWorkerThreads).Index(3).Name("max_worker_threads");
            Map(m => m.MaxCompletionPortThreads).Index(4).Name("max_completion_port_threads");
        }
    }
}