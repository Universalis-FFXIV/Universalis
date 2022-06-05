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

    public Task<TaxRates> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        return _store.GetTaxRates(query.WorldId);
    }

    public Task Update(TaxRates document, TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        return _store.SetTaxRates(query.WorldId, document);
    }
}