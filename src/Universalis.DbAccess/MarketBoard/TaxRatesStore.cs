using System.Linq;
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
        return db.HashSetAsync(worldId.ToString(), new[]
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
        var tasks = new[]
                { "Limsa Lominsa", "Gridania", "Ul'dah", "Ishgard", "Kugane", "Crystarium", "Old Sharlayan", "source" }
            .Select(k => db.HashGetAsync(key, k));
        var values = await Task.WhenAll(tasks);
        return new TaxRates
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
    }
}