using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class UploadCountHistoryDbAccessTests
{
    private class MockDailyUploadCountStore : IDailyUploadCountStore
    {
        private readonly List<long> _counts = new();
        private long _lastPush;
        
        public Task Increment(CancellationToken cancellationToken = default)
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

        public Task<IList<long>> GetUploadCounts(int stop = -1, CancellationToken cancellationToken = default)
        {
            var en = _counts;
            if (stop > -1)
            {
                en = en.Take(stop - 1).ToList();
            }
        
            return Task.FromResult((IList<long>)en);
        }
    }
    
    [Fact]
    public async Task Increment_Works()
    {
        var db = new UploadCountHistoryDbAccess(new MockDailyUploadCountStore());
        await db.Increment();

        var data = await db.GetUploadCounts();
        Assert.Single(data);
        Assert.Equal(1U, data[0]);
    }
}