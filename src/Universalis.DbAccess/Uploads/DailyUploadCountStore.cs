using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class DailyUploadCountStore : IDailyUploadCountStore
{
    private readonly IConnectionMultiplexer _redis;

    public DailyUploadCountStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task Increment(string key, string lastPushKey)
    {
        var db = _redis.GetDatabase();
        
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var lastPush = (long)await db.StringGetAsync(lastPushKey);

        if (now - lastPush > 86400000)
        {
            await db.StringSetAsync(lastPushKey, now);
            await db.ListLeftPushAsync(key, 0);
        }

        // This is probably not good
        var count = (long)await db.ListGetByIndexAsync(key, 0);
        count++;
        await db.ListSetByIndexAsync(key, 0, count);
    }

    public async Task<IList<long>> GetUploadCounts(string key, int stop = -1)
    {
        var db = _redis.GetDatabase();
        var counts = await db.ListRangeAsync(key, stop: stop);
        return counts.Select(c => (long)c).ToList();
    }
}