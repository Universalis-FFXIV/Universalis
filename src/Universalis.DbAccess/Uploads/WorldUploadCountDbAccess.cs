using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class WorldUploadCountDbAccess : IWorldUploadCountDbAccess, IDisposable
{
    public static readonly string Key = "Universalis.WorldUploadCounts";

    private readonly IWorldUploadCountStore _store;
    private readonly ConcurrentDictionary<string, WorldUploadCount> _cache;
    private readonly SemaphoreSlim _initLock;
    private bool _initialized;

    public WorldUploadCountDbAccess(IWorldUploadCountStore store)
    {
        _store = store;
        _cache = new ConcurrentDictionary<string, WorldUploadCount>();
        _initLock = new SemaphoreSlim(1, 1);
        _initialized = false;
    }

    public async Task Increment(WorldUploadCountQuery query, CancellationToken cancellationToken = default)
    {
        await EnsureInitialized(cancellationToken);
        if (_cache.TryGetValue(query.WorldName, out var worldUploadCount))
        {
            // Increments world upload count atomically
            worldUploadCount.Increment();
        }
        else
        {
            // This is not atomic, but it is a rare case
            _cache.TryAdd(query.WorldName, new WorldUploadCount { Count = 1, WorldName = query.WorldName });
        }

        await _store.Increment(Key, query.WorldName);
    }

    public async Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts(CancellationToken cancellationToken = default)
    {
        await EnsureInitialized(cancellationToken);
        return _cache.Values.OrderByDescending(w => w.Count);
    }

    private ValueTask EnsureInitialized(CancellationToken cancellationToken = default)
    {
        return !_initialized ? new ValueTask(Initialize(cancellationToken)) : ValueTask.CompletedTask;
    }

    private async Task Initialize(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (!_initialized)
            {
                var items = await _store.GetWorldUploadCounts(Key);
                foreach (var (world, count) in items)
                {
                    _cache[world] = new WorldUploadCount { Count = count, WorldName = world };
                }

                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Dispose()
    {
        _initLock.Dispose();
        GC.SuppressFinalize(this);
    }
}