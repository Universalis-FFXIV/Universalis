using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Enyim.Caching.Memcached;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class WorldUploadCountStore : IWorldUploadCountStore
{
    private static readonly string RedisKey = "Universalis.WorldUploadCounts";
    private static readonly string CacheKey = "world-upload-counts";

    private readonly IConnectionMultiplexer _redis;
    private readonly IMemcachedCluster _memcached;

    public WorldUploadCountStore(IConnectionMultiplexer redis, IMemcachedCluster memcached)
    {
        _redis = redis;
        _memcached = memcached;
    }

    public async Task Increment(string worldName)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        await db.SortedSetIncrementAsync(RedisKey, worldName, 1);
    }

    public async Task<IList<KeyValuePair<string, long>>> GetWorldUploadCounts()
    {
        // Try getting data from the cache
        var cache = _memcached.GetClient();
        var cacheData1 = await cache.GetAsync<string>(CacheKey);
        if (!string.IsNullOrEmpty(cacheData1))
        {
            var cachedObject = JsonSerializer.Deserialize<IList<KeyValuePair<string, long>>>(cacheData1);
            if (cachedObject != null)
            {
                return cachedObject;
            }
        }

        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var counts = await db.SortedSetRangeByRankWithScoresAsync(RedisKey, order: Order.Descending);
        var result = counts.Select(entry => new KeyValuePair<string, long>(entry.Element, (long)entry.Score)).ToList();

        // Store data in the cache for a brief period of time.
        // It's unlikely that anyone needs this data to be available immediately.
        var cacheData2 = JsonSerializer.Serialize(result);
        await cache.SetAsync(CacheKey, cacheData2, Expiration.From(TimeSpan.FromSeconds(10)));

        return result;
    }
}