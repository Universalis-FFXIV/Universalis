using System;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.Application.Caching;

public interface ICache<in TKey, TValue> where TKey : IEquatable<TKey>
{
    public int Count { get; }

    public Task Set(TKey key, TValue value, CancellationToken cancellationToken = default);

    public Task<TValue> Get(TKey key, CancellationToken cancellationToken = default);

    public Task<bool> Delete(TKey key, CancellationToken cancellationToken = default);
}