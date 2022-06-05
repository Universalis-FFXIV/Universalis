using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockTaxRatesDbAccess : ITaxRatesDbAccess
{
    private readonly Dictionary<uint, TaxRates> _collection = new();

    public Task<TaxRates> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        return !_collection.TryGetValue(query.WorldId, out var taxRates)
            ? Task.FromResult<TaxRates>(null)
            : Task.FromResult(taxRates);
    }

    public async Task Update(TaxRates document, TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        _collection[query.WorldId] = document;
    }
}