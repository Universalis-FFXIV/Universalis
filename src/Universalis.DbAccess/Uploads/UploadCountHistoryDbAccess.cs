using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public class UploadCountHistoryDbAccess : IUploadCountHistoryDbAccess, IDisposable
{
    public static readonly string Key = "Universalis.DailyUploads";
    public static readonly string KeyLastPush = "Universalis.DailyUploadsLastPush";

    private readonly IDailyUploadCountStore _store;
    private readonly ReaderWriterLockSlim _cacheLock;
    private IList<long> _cached;

    public UploadCountHistoryDbAccess(IDailyUploadCountStore store)
    {
        _store = store;
        _cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    public async Task Increment(CancellationToken cancellationToken = default)
    {
        await _store.Increment(Key, KeyLastPush, cancellationToken);

        _cacheLock.EnterWriteLock();
        try
        {
            _cached = await GetUploadCounts(cancellationToken: cancellationToken);
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    public async ValueTask<IList<long>> GetUploadCounts(int stop = -1, CancellationToken cancellationToken = default)
    {
        if (_cacheLock.TryEnterReadLock(50))
        {
            try
            {
                if (_cached is not null)
                {
                    return stop > -1 ? _cached.Take(stop + 1).ToList() : _cached.ToList();
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
            _cached = await _store.GetUploadCounts(Key, cancellationToken: cancellationToken);
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