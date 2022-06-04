using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockTaxRatesDbAccess : ITaxRatesDbAccess
{
    private readonly Dictionary<uint, TaxRatesSimple> _collection = new();

    public Task<TaxRatesSimple> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        return !_collection.TryGetValue(query.WorldId, out var taxRates)
            ? Task.FromResult<TaxRatesSimple>(null)
            : Task.FromResult(taxRates);
    }

    public async Task Update(TaxRatesSimple document, TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        _collection[query.WorldId] = document;
    }
}