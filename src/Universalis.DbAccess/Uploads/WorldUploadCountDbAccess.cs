using System;
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
    private readonly ReaderWriterLockSlim _cacheLock;
    private IList<WorldUploadCount> _cached;

    public WorldUploadCountDbAccess(IWorldUploadCountStore store)
    {
        _store = store;
        _cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    public async Task Increment(WorldUploadCountQuery query, CancellationToken cancellationToken = default)
    {
        await _store.Increment(Key, query.WorldName);

        _cacheLock.EnterWriteLock();
        try
        {
            _cached = (await GetWorldUploadCounts(cancellationToken)).ToList();
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    public async ValueTask<IEnumerable<WorldUploadCount>> GetWorldUploadCounts(
        CancellationToken cancellationToken = default)
    {
        if (_cacheLock.TryEnterReadLock(50))
        {
            try
            {
                if (_cached is not null)
                {
                    return _cached.ToList();
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
            var counts = await _store.GetWorldUploadCounts(Key);
            _cached = counts
                .Select(c => new WorldUploadCount
                {
                    WorldName = c.Key,
                    Count = c.Value,
                })
                .ToList();
            return _cached;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _cacheLock.Dispose();
        GC.SuppressFinalize(this);
    }
}