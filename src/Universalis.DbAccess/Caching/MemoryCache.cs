using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Universalis.DbAccess.Caching;

public class MemoryCache<TKey, TValue> : ICache<TKey, TValue> where TKey : IEquatable<TKey> where TValue : class
{
    private readonly ReaderWriterLockSlim _lock;
    private readonly CacheEntry<TKey, TValue>[] _data;
    private readonly IDictionary<TKey, int> _idMap;
    private readonly Stack<int> _freeEntries;

    public int Capacity { get; }

    public MemoryCache(int size)
    {
        _lock = new ReaderWriterLockSlim();
        _data = new CacheEntry<TKey, TValue>[size];
        _idMap = new Dictionary<TKey, int>(size);
        _freeEntries = new Stack<int>(Enumerable.Range(0, size));
        _freeEntries.TrimExcess();

        Capacity = size;
    }

    public void Set(TKey key, TValue value)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            // Check if this key already has an entry associated with it
            if (_idMap.TryGetValue(key, out var idx))
            {
                _data[idx].Dirty = true;
                _data[idx].Value = value;
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                // Get a data array index
                if (!_freeEntries.TryPop(out var nextIdx))
                {
                    nextIdx = Evict();
                }

                // Set the cache entry
                _idMap[key] = nextIdx;
                _data[nextIdx] = new CacheEntry<TKey, TValue>
                {
                    Key = key,
                    Value = value,
                };
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public TValue Get(TKey key)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_idMap.TryGetValue(key, out var idx)) return null;
            var val = _data[idx];
            val.Dirty = true;
            return JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(val.Value));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Delete(TKey key)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_idMap.TryGetValue(key, out var idx))
            {
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                _idMap.Remove(key);
                _freeEntries.Push(idx);
                _data[idx] = null;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Evicts an entry from the cache, returning the evicted entry's index in the data array.
    /// </summary>
    private int Evict()
    {
        while (true)
        {
            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i] == null) continue;

                if (!_data[i].Dirty)
                {
                    _idMap.Remove(_data[i].Key);
                    _freeEntries.Push(i);
                    _data[i] = null;
                    return i;
                }

                _data[i].Dirty = false;
            }
        }
    }

    private class CacheEntry<TCacheKey, TCacheValue>
    {
        public bool Dirty;

        public TCacheKey Key;

        public TCacheValue Value;
    }
}