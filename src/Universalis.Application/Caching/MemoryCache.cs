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
    private readonly ReaderWriterLockSlim _lock;
    private readonly CacheEntry<TKey, TValue>[] _data;
    private readonly IDictionary<TKey, int> _idMap;
    private readonly Stack<int> _freeEntries;
    private readonly SimplePriorityQueue<int, CacheEntry<TKey, TValue>> _hits;

    public int Count => GetCount();

    public MemoryCache(int size)
    {
        _lock = new ReaderWriterLockSlim();
        _data = new CacheEntry<TKey, TValue>[size];
        _idMap = new Dictionary<TKey, int>();
        _freeEntries = new Stack<int>(Enumerable.Range(0, size));
        _freeEntries.TrimExcess();
        _hits = new SimplePriorityQueue<int, CacheEntry<TKey, TValue>>((a, b) => a.Hits - b.Hits);
    }

    public virtual ValueTask Set(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        var keyCopy = (TKey)key.Clone();
        var valCopy = (TValue)value.Clone();
        if (keyCopy == null || valCopy == null) throw new ArgumentException("key or value de/serialized to null.");

        _lock.EnterWriteLock();
        try
        {
            // Check if this key already has an entry associated with it
            // that we can reuse
            if (_idMap.TryGetValue(keyCopy, out var idx))
            {
                _data[idx].ResetHits();
                _data[idx].Value = valCopy;
                _hits.UpdatePriority(idx, _data[idx]);
                return ValueTask.CompletedTask;
            }

            CleanAdd(keyCopy, valCopy);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return ValueTask.CompletedTask;
    }

    public virtual ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_idMap.TryGetValue(key, out var idx)) return ValueTask.FromResult<TValue>(null);

            var val = _data[idx];
            val.IncrementHits();
            _hits.UpdatePriority(idx, val);

            var valCopy = (TValue)val.Value.Clone();
            return ValueTask.FromResult(valCopy);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public virtual ValueTask<bool> Delete(TKey key, CancellationToken cancellationToken = default)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_idMap.TryGetValue(key, out var idx))
            {
                return ValueTask.FromResult(false);
            }

            _lock.EnterWriteLock();
            try
            {
                CleanRemove(idx);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return ValueTask.FromResult(true);
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
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

        _data[idx] = default;
        _idMap.Remove(val.Key);
        _hits.Remove(idx);
        _freeEntries.Push(idx);
    }

    private struct CacheEntry<TCacheKey, TCacheValue>
    {
        public int Hits => _hits;

        public TCacheKey Key;

        public TCacheValue Value;

        private int _hits;

        public void IncrementHits()
        {
            Interlocked.Increment(ref _hits);
        }

        public void ResetHits()
        {
            Interlocked.Exchange(ref _hits, 0);
        }
    }
}