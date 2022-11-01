using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class TaxRatesDbAccess : ITaxRatesDbAccess
{
    private readonly ITaxRatesStore _store;

    public TaxRatesDbAccess(ITaxRatesStore store)
    {
        _store = store;
    }

    public async ValueTask<TaxRates> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        var taxRates = await _store.GetTaxRates(query.WorldId);
        return taxRates;
    }

    public async Task Update(TaxRates document, TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        await _store.SetTaxRates(query.WorldId, document);
    }
}