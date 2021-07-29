using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess
{
    public class MockTaxRatesDbAccess : ITaxRatesDbAccess
    {
        private readonly Dictionary<uint, TaxRates> _collection = new();

        public Task Create(TaxRates document)
        {
            _collection.Add(document.WorldId, document);
            return Task.CompletedTask;
        }

        public Task<TaxRates> Retrieve(TaxRatesQuery query)
        {
            return !_collection.TryGetValue(query.WorldId, out var taxRates)
                ? Task.FromResult<TaxRates>(null)
                : Task.FromResult(taxRates);
        }

        public async Task Update(TaxRates document, TaxRatesQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public Task Delete(TaxRatesQuery query)
        {
            _collection.Remove(query.WorldId);
            return Task.CompletedTask;
        }
    }
}