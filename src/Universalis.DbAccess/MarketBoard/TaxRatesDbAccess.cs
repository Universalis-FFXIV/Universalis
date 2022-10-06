using System.Threading;
using System.Threading.Tasks;
using Universalis.Common.Caching;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class TaxRatesDbAccess : ITaxRatesDbAccess
{
    private readonly ITaxRatesStore _store;
    private readonly ICache<TaxRatesQuery, TaxRates> _cache;

    public TaxRatesDbAccess(ITaxRatesStore store)
    {
        _store = store;
        _cache = new MemoryCache<TaxRatesQuery, TaxRates>(110);
    }

    public async ValueTask<TaxRates> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        var cachedTaxRates = await _cache.Get(query, cancellationToken);
        if (cachedTaxRates != null)
        {
            return cachedTaxRates;
        }

        var taxRates = await _store.GetTaxRates(query.WorldId);
        await _cache.Set(query, taxRates, cancellationToken);
        return taxRates;
    }

    public async Task Update(TaxRates document, TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        await _cache.Set(query, document, cancellationToken);
        await _store.SetTaxRates(query.WorldId, document);
    }
}