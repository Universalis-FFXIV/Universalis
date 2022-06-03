using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockUploadCountHistoryDbAccess : IUploadCountHistoryDbAccess
{
    private readonly List<long> _counts = new();
    private long _lastPush;

    public Task Increment()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - _lastPush > 86400000)
        {
            _counts.Insert(0, 0);
            _lastPush = now;
        }

        _counts[0]++;
        
        return Task.CompletedTask;
    }

    public Task<IList<long>> GetUploadCounts(int count = -1)
    {
        var en = _counts;
        if (count > -1)
        {
            en = en.Take(count).ToList();
        }
        
        return Task.FromResult((IList<long>)en);
    }
}