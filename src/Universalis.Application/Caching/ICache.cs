using System;
using System.Threading.Tasks;

namespace Universalis.Application.Caching;

public interface ICache<in TKey, TValue> where TKey : IEquatable<TKey>
{
    public int Count { get; }

    public Task Set(TKey key, TValue value);

    public Task<TValue> Get(TKey key);

    public Task<bool> Delete(TKey key);
}