using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockTaxRatesDbAccess : ITaxRatesDbAccess
{
    private readonly Dictionary<int, TaxRates> _collection = new();

    public ValueTask<TaxRates> Retrieve(TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        return !_collection.TryGetValue(query.WorldId, out var taxRates)
            ? ValueTask.FromResult<TaxRates>(null)
            : ValueTask.FromResult(taxRates);
    }

    public Task Update(TaxRates document, TaxRatesQuery query, CancellationToken cancellationToken = default)
    {
        _collection[query.WorldId] = document;
        return Task.CompletedTask;
    }
}