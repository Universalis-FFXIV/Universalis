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
        var valCopy = JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(value));

        _lock.EnterWriteLock();
        try
        {
            // Check if this key already has an entry associated with it
            if (_idMap.TryGetValue(key, out var idx))
            {
                _data[idx].Dirty = false;
                _data[idx].Value = valCopy;
                return;
            }

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
                Value = valCopy,
            };
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public TValue Get(TKey key)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_idMap.TryGetValue(key, out var idx)) return null;

            _lock.EnterWriteLock();
            try
            {
                var val = _data[idx];
                val.Dirty = true;
                return JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(val.Value));
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