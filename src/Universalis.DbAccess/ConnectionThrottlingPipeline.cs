using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.DbAccess
{
    // From https://stackoverflow.com/a/57517920/14226597
    public class ConnectionThrottlingPipeline : IConnectionThrottlingPipeline
    {
        private readonly Semaphore _openConnectionSemaphore;

        public ConnectionThrottlingPipeline(IMongoClient client)
        {
            _openConnectionSemaphore = new Semaphore(client.Settings.MaxConnectionPoolSize / 2,
                client.Settings.MaxConnectionPoolSize / 2);
        }

        public Task<T> AddRequest<T>(Func<Task<T>> task)
        {
            _openConnectionSemaphore.WaitOne();
            try
            {
                return task();
            }
            finally
            {
                _openConnectionSemaphore.Release();
            }
        }

        public Task AddRequest(Func<Task> task)
        {
            _openConnectionSemaphore.WaitOne();
            try
            {
                return task();
            }
            finally
            {
                _openConnectionSemaphore.Release();
            }
        }
    }
}