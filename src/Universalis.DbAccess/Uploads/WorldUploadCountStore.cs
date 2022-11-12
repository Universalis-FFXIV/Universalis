using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Uploads;

public class WorldUploadCountStore : IWorldUploadCountStore
{
    private static readonly string RedisKey = "Universalis.WorldUploadCounts";
    private static readonly string CacheKey = "world-upload-counts";

    private readonly IPersistentRedisMultiplexer _redis;
    private readonly ICacheRedisMultiplexer _cache;
    private readonly ILogger<WorldUploadCountStore> _logger;

    public WorldUploadCountStore(IPersistentRedisMultiplexer redis, ICacheRedisMultiplexer cache, ILogger<WorldUploadCountStore> logger)
    {
        _redis = redis;
        _cache = cache;
        _logger = logger;
    }

    public async Task Increment(string worldName)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        await db.SortedSetIncrementAsync(RedisKey, worldName, 1, CommandFlags.FireAndForget);

        // Write through to the cache
        var cache = _redis.GetDatabase(RedisDatabases.Cache.Stats);
        await cache.SortedSetIncrementAsync(CacheKey, worldName, 1, CommandFlags.FireAndForget);
    }

    public async Task<IList<KeyValuePair<string, long>>> GetWorldUploadCounts()
    {
        // Try to fetch data from the cache
        var cache = _cache.GetDatabase(RedisDatabases.Cache.Stats);
        try
        {
            
            if (await cache.KeyExistsAsync(CacheKey))
            {
                var cached = await cache.SortedSetRangeByRankWithScoresAsync(CacheKey, order: Order.Descending);
                var cachedObject = cached.Select(entry => new KeyValuePair<string, long>(entry.Element, (long)entry.Score)).ToList();
                return cachedObject;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve world upload counts from cache");
        }

        // Fetch data from the database
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var counts = await db.SortedSetRangeByRankWithScoresAsync(RedisKey, order: Order.Descending);
        var result = counts.Select(entry => new KeyValuePair<string, long>(entry.Element, (long)entry.Score)).ToList();

        // Store the result in the cache
        try
        {
            var tasks = counts.Select(entry => cache.SortedSetAddAsync(CacheKey, entry.Element, (long)entry.Score));
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store world upload counts in cache");
        }

        return result;
    }
}