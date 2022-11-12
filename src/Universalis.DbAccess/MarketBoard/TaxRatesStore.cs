using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class TaxRatesStore : ITaxRatesStore
{
    /// <summary>
    /// The primary store for tax rates data.
    /// </summary>
    private readonly IPersistentRedisMultiplexer _redis;

    public TaxRatesStore(IPersistentRedisMultiplexer redis)
    {
        _redis = redis;
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

        await dbSet;
    }

    public async Task<TaxRates> GetTaxRates(uint worldId)
    {
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

        return taxRates;
    }

    private static string GetCacheKey(uint worldId)
    {
        return $"tax-rates:${worldId}";
    }
}