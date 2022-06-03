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

        // Create the last push time key
        if (lastPush == 0)
        {
            var t0 = db.CreateTransaction();
            t0.AddCondition(Condition.KeyNotExists(lastPushKey));
            _ = t0.StringSetAsync(lastPushKey, 0);
            await t0.ExecuteAsync();
        }

        // Push a new counter if the date has rolled over
        if (now - lastPush > 86400000)
        {
            var t1 = db.CreateTransaction();
            t1.AddCondition(Condition.StringEqual(lastPushKey, lastPush));
            _ = t1.StringSetAsync(lastPushKey, now);
            _ = t1.ListLeftPushAsync(key, 0);
            await t1.ExecuteAsync();

            lastPush = now;
        }
        
        // Increment the counter
        var count = (long)await db.ListGetByIndexAsync(key, 0);

        var t2 = db.CreateTransaction();
        // Don't accidentally copy the last count into today's count
        t2.AddCondition(Condition.StringEqual(lastPushKey, lastPush));
        count++;
        _ = t2.ListSetByIndexAsync(key, 0, count);
        await t2.ExecuteAsync();
    }

    public async Task<IList<long>> GetUploadCounts(string key, int stop = -1)
    {
        var db = _redis.GetDatabase();
        var counts = await db.ListRangeAsync(key, stop: stop);
        return counts.Select(c => (long)c).ToList();
    }
}