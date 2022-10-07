using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Priority_Queue;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess, IDisposable
{
    public static readonly int MaxItems = 200;

    public static readonly string Key = "Universalis.RecentlyUpdated";

    private readonly IRecentlyUpdatedItemsStore _store;
    private readonly SimplePriorityQueue<uint, double> _cache;
    private readonly SemaphoreSlim _initLock;
    private bool _initialized;

    public RecentlyUpdatedItemsDbAccess(IRecentlyUpdatedItemsStore store)
    {
        _store = store;
        _cache = new SimplePriorityQueue<uint, double>();
        _initLock = new SemaphoreSlim(1, 1);
        _initialized = false;
    }

    public async Task<RecentlyUpdatedItems> Retrieve(CancellationToken cancellationToken = default)
    {
        await EnsureInitialized(cancellationToken);
        return new RecentlyUpdatedItems { Items = _cache.Take(MaxItems).ToList() };
    }

    public async Task Push(uint itemId, CancellationToken cancellationToken = default)
    {
        await EnsureInitialized(cancellationToken);
        var t = Convert.ToDouble(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        if (!_cache.TryUpdatePriority(itemId, t))
        {
            _cache.EnqueueWithoutDuplicates(itemId, t);
        }

        await _store.SetItem(Key, itemId, t);
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
                var initialData = await _store.GetAllItems(Key, MaxItems - 1);
                foreach (var (item, t) in initialData)
                {
                    _cache.Enqueue(item, t);
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