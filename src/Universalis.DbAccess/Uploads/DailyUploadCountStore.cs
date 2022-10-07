using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public async Task Increment(string key, string lastPushKey, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var lastPush = (long)await db.StringGetAsync(lastPushKey);

        // Create the last push time key
        if (lastPush == 0)
        {
            await db.StringSetAsync(lastPushKey, 0, when: When.NotExists);
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

    public async Task<IList<long>> GetUploadCounts(string key, int stop = -1, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var counts = await db.ListRangeAsync(key, stop: stop);
        return counts.Select(c => (long)c).ToList();
    }
}