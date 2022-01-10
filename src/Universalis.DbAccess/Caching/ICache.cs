using System;

namespace Universalis.DbAccess.Caching;

public interface ICache<in TKey, TValue> where TKey : IEquatable<TKey>
{
    public void Set(TKey key, TValue value);

    public TValue Get(TKey key);
}