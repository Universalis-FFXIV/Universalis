using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class WorldUploadCountStore : IWorldUploadCountStore
{
    private static readonly string RedisKey = "Universalis.WorldUploadCounts";
    private static readonly string CacheKey = "world-upload-counts";

    private readonly IPersistentRedisMultiplexer _redis;

    public WorldUploadCountStore(IPersistentRedisMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task Increment(string worldName)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        await db.SortedSetIncrementAsync(RedisKey, worldName, 1);
    }

    public async Task<IList<KeyValuePair<string, long>>> GetWorldUploadCounts()
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var counts = await db.SortedSetRangeByRankWithScoresAsync(RedisKey, order: Order.Descending);
        var result = counts.Select(entry => new KeyValuePair<string, long>(entry.Element, (long)entry.Score)).ToList();
        return result;
    }
}