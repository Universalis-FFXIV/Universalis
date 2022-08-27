using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Priority_Queue;

namespace Universalis.Application.Caching;

public class MemoryCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : IEquatable<TKey>, ICopyable where TValue : class, ICopyable
{
    private readonly object _lock;
    private readonly CacheEntry<TKey, TValue>[] _data;
    private readonly IDictionary<TKey, int> _idMap;
    private readonly Stack<int> _freeEntries;
    private readonly SimplePriorityQueue<int, CacheEntry<TKey, TValue>> _hits;

    public int Count => GetCount();

    public MemoryCache(int size)
    {
        _lock = new object();
        _data = new CacheEntry<TKey, TValue>[size];
        _idMap = new Dictionary<TKey, int>();
        _freeEntries = new Stack<int>(Enumerable.Range(0, size));
        _freeEntries.TrimExcess();
        _hits = new SimplePriorityQueue<int, CacheEntry<TKey, TValue>>((a, b) => a.Hits - b.Hits);
    }

    public virtual Task Set(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        var keyCopy = (TKey)key.Clone();
        var valCopy = (TValue)value.Clone();
        if (keyCopy == null || valCopy == null) throw new ArgumentException("key or value de/serialized to null.");

        Monitor.Enter(_lock);
        try
        {
            // Check if this key already has an entry associated with it
            // that we can reuse
            if (_idMap.TryGetValue(keyCopy, out var idx))
            {
                _data[idx].Hits = 0;
                _data[idx].Value = valCopy;
                _hits.UpdatePriority(idx, _data[idx]);
                return Task.CompletedTask;
            }

            CleanAdd(keyCopy, valCopy);
        }
        finally
        {
            Monitor.Exit(_lock);
        }

        return Task.CompletedTask;
    }

    public virtual Task<TValue> Get(TKey key, CancellationToken cancellationToken = default)
    {
        Monitor.Enter(_lock);
        try
        {
            if (!_idMap.TryGetValue(key, out var idx)) return Task.FromResult<TValue>(null);

            var val = _data[idx];
            val.Hits++;
            _hits.UpdatePriority(idx, val);

            var valCopy = (TValue)val.Value.Clone();
            return Task.FromResult(valCopy);
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    public virtual Task<bool> Delete(TKey key, CancellationToken cancellationToken = default)
    {
        Monitor.Enter(_lock);
        try
        {
            if (!_idMap.TryGetValue(key, out var idx))
            {
                return Task.FromResult(false);
            }

            CleanRemove(idx);

            return Task.FromResult(true);
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// Evicts an entry from the cache, returning the evicted entry's index to the free entry stack.
    /// </summary>
    /// <returns>true if an entry was evicted; otherwise false.</returns>
    protected virtual bool Evict()
    {
        if (Count == 0)
        {
            return false;
        }

        // Find the entry with the *highest* number of hits and remove it.
        // We assume that the older an item is, the more likely it is to be
        // checked soon.
        // https://en.wikipedia.org/wiki/Cache_replacement_policies#Most_recently_used_(MRU)
        CleanRemove(_hits.First);
        return true;
    }

    private int GetCount()
    {
        Monitor.Enter(_lock);
        try
        {
            return _idMap.Count;
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    private void CleanAdd(TKey key, TValue value)
    {
        // Get a data array index
        if (!_freeEntries.TryPop(out var nextIdx))
        {
            while (!Evict())
            {
            }

            nextIdx = _freeEntries.Pop();
        }

        // Set the cache entry
        var newEntry = new CacheEntry<TKey, TValue>
        {
            Key = key,
            Value = value,
        };

        _idMap.Add(key, nextIdx);
        _hits.Enqueue(nextIdx, newEntry);
        _data[nextIdx] = newEntry;
    }

    private void CleanRemove(int idx)
    {
        var val = _data[idx];

        _data[idx] = null;
        _idMap.Remove(val.Key);
        _hits.Remove(idx);
        _freeEntries.Push(idx);
    }

    private class CacheEntry<TCacheKey, TCacheValue>
    {
        public int Hits;

        public TCacheKey Key;

        public TCacheValue Value;
    }
}