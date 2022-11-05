﻿using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Enyim.Caching.Memcached;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class TaxRatesStore : ITaxRatesStore
{
    /// <summary>
    /// The primary store for tax rates data.
    /// </summary>
    private readonly IConnectionMultiplexer _redis;

    /// <summary>
    /// The near-cache for tax rates data. Microsoft DI doesn't allow for injecting
    /// multiple objects of the same interface type, so we can have some fun with a
    /// messier tech stack.
    /// 
    /// This should be replaced with a Redis cluster later, but making that work with
    /// Microsoft DI will require a moderate refactor.
    /// 
    /// This Memcached client uses BinaryFormatter for complex objects, which ASP.NET
    /// Core completely restricts (as it should). To work around this, JSON is used
    /// for this instead.
    /// </summary>
    private readonly IMemcachedCluster _memcached;

    private readonly ILogger<TaxRatesStore> _logger;

    public TaxRatesStore(IConnectionMultiplexer redis, IMemcachedCluster memcached, ILogger<TaxRatesStore> logger)
    {
        _redis = redis;
        _memcached = memcached;
        _logger = logger;
    }

    public async Task SetTaxRates(uint worldId, TaxRates taxRates)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.TaxRates);
        var dbSet = db.HashSetAsync(worldId.ToString(), new[]
        {
            new HashEntry("Limsa Lominsa", taxRates.LimsaLominsa),
            new HashEntry("Gridania", taxRates.Gridania),
            new HashEntry("Ul'dah", taxRates.Uldah),
            new HashEntry("Ishgard", taxRates.Ishgard),
            new HashEntry("Kugane", taxRates.Kugane),
            new HashEntry("Crystarium", taxRates.Crystarium),
            new HashEntry("Old Sharlayan", taxRates.OldSharlayan),
            new HashEntry("source", taxRates.UploadApplicationName),
        });

        // Write through to the cache
        var cache = _memcached.GetClient();
        var cacheData = JsonSerializer.Serialize(taxRates);
        var cacheSet = cache.SetAsync(GetCacheKey(worldId), cacheData);

        await Task.WhenAll(dbSet, cacheSet);
    }

    public async Task<TaxRates> GetTaxRates(uint worldId)
    {
        // Try to get the tax rates from the cache
        var cache = _memcached.GetClient();
        var cacheData1 = await cache.GetWithResultAsync<string>(GetCacheKey(worldId));
        if (cacheData1.Success)
        {
            try
            {
                var cacheObject = JsonSerializer.Deserialize<TaxRates>(cacheData1.Value);
                if (cacheObject != null)
                {
                    return cacheObject;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to deserialize object: {JsonData}", cacheData1.Value);
            }
        }

        // Fetch the tax rates from the database
        var db = _redis.GetDatabase(RedisDatabases.Instance0.TaxRates);
        var key = worldId.ToString();
        var tasks = new[]
                { "Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard", "Kugane", "Crystarium", "Old Sharlayan", "source" }
            .Select(k => db.HashGetAsync(key, k));
        var values = await Task.WhenAll(tasks);
        var taxRates = new TaxRates
        {
            LimsaLominsa = (int)values[0],
            Gridania = (int)values[1],
            Uldah = (int)values[2],
            Ishgard = (int)values[3],
            Kugane = (int)values[4],
            Crystarium = (int)values[5],
            OldSharlayan = (int)values[6],
            UploadApplicationName = values[7],
        };

        // Cache the data
        var cacheData2 = JsonSerializer.Serialize(taxRates);
        await cache.SetAsync(GetCacheKey(worldId), cacheData2);
        return taxRates;
    }

    private static string GetCacheKey(uint worldId)
    {
        return $"tax-rates:${worldId}";
    }
}