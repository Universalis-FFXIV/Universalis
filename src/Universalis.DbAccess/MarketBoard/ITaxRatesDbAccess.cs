using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ITaxRatesDbAccess
{
    public Task Create(TaxRates document, CancellationToken cancellationToken = default);

    public Task<TaxRates> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default);

    public Task Update(TaxRates document, TaxRatesQuery query, CancellationToken cancellationToken = default);

    public Task Delete(TaxRatesQuery query, CancellationToken cancellationToken = default);
}