using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class TaxRatesStore : ITaxRatesStore
{
    private readonly IConnectionMultiplexer _redis;

    public TaxRatesStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task SetTaxRates(uint worldId, TaxRates taxRates)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.TaxRates);
        return db.HashSetAsync(worldId.ToString(), new []
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
    }

    public async Task<TaxRates> GetTaxRates(uint worldId)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.TaxRates);
        var key = worldId.ToString();
        return new TaxRates
        {
            LimsaLominsa = (int)await db.HashGetAsync(key, "Limsa Lominsa"),
            Gridania = (int)await db.HashGetAsync(key, "Gridania"),
            Uldah = (int)await db.HashGetAsync(key, "Ul'dah"),
            Ishgard = (int)await db.HashGetAsync(key, "Ishgard"),
            Kugane = (int)await db.HashGetAsync(key, "Kugane"),
            Crystarium = (int)await db.HashGetAsync(key, "Crystarium"),
            OldSharlayan = (int)await db.HashGetAsync(key, "Old Sharlayan"),
            UploadApplicationName = await db.HashGetAsync(key, "source"),
        };
    }
}