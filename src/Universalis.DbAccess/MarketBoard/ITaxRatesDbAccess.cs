using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ITaxRatesDbAccess
{
    public ValueTask<TaxRates> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default);

    public Task Update(TaxRates document, TaxRatesQuery query, CancellationToken cancellationToken = default);
}