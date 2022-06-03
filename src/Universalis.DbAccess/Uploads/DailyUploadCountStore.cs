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
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var lastPush = (long)await db.StringGetAsync(lastPushKey);

        if (now - lastPush > 86400000)
        {
            var t1 = db.CreateTransaction();
            t1.AddCondition(Condition.StringEqual(lastPushKey, lastPush));
            await t1.StringSetAsync(lastPushKey, now);
            await t1.ListLeftPushAsync(key, 0);
            await t1.ExecuteAsync();
        }

        var t2 = db.CreateTransaction();
        var count = (long)await t2.ListGetByIndexAsync(key, 0);
        count++;
        await t2.ListSetByIndexAsync(key, 0, count);
        await t2.ExecuteAsync();
    }

    public async Task<IList<long>> GetUploadCounts(string key, int stop = -1)
    {
        var db = _redis.GetDatabase();
        var counts = await db.ListRangeAsync(key, stop: stop);
        return counts.Select(c => (long)c).ToList();
    }
}