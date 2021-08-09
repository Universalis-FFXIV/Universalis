using System;
using System.Threading.Tasks;

namespace Universalis.DbAccess
{
    public interface IConnectionThrottlingPipeline
    {
        public Task<T> AddRequest<T>(Func<Task<T>> task);

        public Task AddRequest(Func<Task> task);
    }
}