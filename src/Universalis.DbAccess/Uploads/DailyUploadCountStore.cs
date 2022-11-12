using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class DailyUploadCountStore : IDailyUploadCountStore
{
    private static readonly string RedisKey = "Universalis.DailyUploads";
    private static readonly string RedisLastPushKey = "Universalis.DailyUploadsLastPush";

    private readonly IPersistentRedisMultiplexer _redis;

    public DailyUploadCountStore(IPersistentRedisMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task Increment()
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var lastPush = (long)await db.StringGetAsync(RedisLastPushKey);

        // Create the last push time key
        if (lastPush == 0)
        {
            await db.StringSetAsync(RedisLastPushKey, 0, when: When.NotExists);
        }

        // Push a new counter if the date has rolled over
        if (now - lastPush > 86400000)
        {
            var t1 = db.CreateTransaction();
            t1.AddCondition(Condition.StringEqual(RedisLastPushKey, lastPush));
            _ = t1.StringSetAsync(RedisLastPushKey, now);
            _ = t1.ListLeftPushAsync(RedisKey, 0);
            await t1.ExecuteAsync();

            lastPush = now;
        }
        
        // Increment the counter
        var count = (long)await db.ListGetByIndexAsync(RedisKey, 0);

        var t2 = db.CreateTransaction();
        // Don't accidentally copy the last count into today's count
        t2.AddCondition(Condition.StringEqual(RedisLastPushKey, lastPush));
        count++;
        _ = t2.ListSetByIndexAsync(RedisKey, 0, count);
        await t2.ExecuteAsync();
    }

    public async Task<IList<long>> GetUploadCounts(int stop = -1)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var counts = await db.ListRangeAsync(RedisKey, stop: stop);
        var result = counts.Select(c => (long)c).ToList();
        return result;
    }
}