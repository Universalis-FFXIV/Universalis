using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Enyim.Caching.Memcached;
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

    public TaxRatesStore(IConnectionMultiplexer redis, IMemcachedCluster memcached)
    {
        _redis = redis;
        _memcached = memcached;
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
        var cacheData = JsonSerializer.Serialize(taxRates.Clone());
        var cacheSet = cache.SetAsync(GetCacheKey(worldId), cacheData);

        await Task.WhenAll(dbSet, cacheSet);
    }

    public async Task<TaxRates> GetTaxRates(uint worldId)
    {
        // Try to get the tax rates from the cache
        var cache = _memcached.GetClient();
        var cacheData1 = await cache.GetAsync<string>(GetCacheKey(worldId));
        if (!string.IsNullOrEmpty(cacheData1))
        {
            var cachedObject = JsonSerializer.Deserialize<TaxRates>(cacheData1);
            if (cachedObject != null)
            {
                return cachedObject;
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
        var cacheData2 = JsonSerializer.Serialize(taxRates.Clone());
        await cache.SetAsync(GetCacheKey(worldId), cacheData2);
        return taxRates;
    }

    private static string GetCacheKey(uint worldId)
    {
        return $"tax-rates:${worldId}";
    }
}