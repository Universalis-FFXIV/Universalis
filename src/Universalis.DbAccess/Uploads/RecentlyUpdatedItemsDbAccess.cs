using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess, IDisposable
{
    public static readonly int MaxItems = 200;

    public static readonly string Key = "Universalis.RecentlyUpdated";

    private readonly IRecentlyUpdatedItemsStore _store;
    private readonly ReaderWriterLockSlim _cacheLock;
    private RecentlyUpdatedItems _cached;

    public RecentlyUpdatedItemsDbAccess(IRecentlyUpdatedItemsStore store)
    {
        _store = store;
        _cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    public async ValueTask<RecentlyUpdatedItems> Retrieve(CancellationToken cancellationToken = default)
    {
        if (_cacheLock.TryEnterReadLock(50))
        {
            try
            {
                if (_cached is not null)
                {
                    return _cached;
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        _cacheLock.EnterWriteLock();
        try
        {
            var items = await _store.GetAllItems(Key, MaxItems - 1);
            return new RecentlyUpdatedItems
            {
                Items = items.Take(MaxItems).Select(i => i.Key).ToList(),
            };
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    public async Task Push(uint itemId, CancellationToken cancellationToken = default)
    {
        _cacheLock.EnterWriteLock();
        try
        {
            // When we update the cached data, we need to handle sorting manually
            // instead of allowing Redis to do it for us.
            _cached ??= await Retrieve(cancellationToken);

            if (_cached.Items.Contains(itemId))
            {
                var idx = _cached.Items.IndexOf(itemId);
                _cached.Items.RemoveAt(idx);
            }

            _cached.Items.Insert(0, itemId);
            if (_cached.Items.Count > MaxItems)
            {
                _cached.Items = _cached.Items.Take(MaxItems).ToList();
            }
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        await _store.SetItem(Key, itemId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public void Dispose()
    {
        _cacheLock.Dispose();
        GC.SuppressFinalize(this);
    }
}