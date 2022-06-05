using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

public class TaxRatesDbAccessTests
{
    private class MockTaxRatesStore : ITaxRatesStore
    {
        private readonly Dictionary<uint, TaxRates> _taxRates = new();

        public Task SetTaxRates(uint worldId, TaxRates taxRates)
        {
            _taxRates[worldId] = taxRates;
            return Task.CompletedTask;
        }

        public Task<TaxRates> GetTaxRates(uint worldId)
        {
            return _taxRates.TryGetValue(worldId, out var rates)
                ? Task.FromResult(rates)
                : Task.FromResult(new TaxRates());
        }
    }

    [Fact]
    public async Task Retrieve_DoesNotThrow()
    {
        var db = new TaxRatesDbAccess(new MockTaxRatesStore());
        await db.Retrieve(new TaxRatesQuery { WorldId = 74 });
    }

    [Fact]
    public async Task Update_DoesNotThrow()
    {
        var db = new TaxRatesDbAccess(new MockTaxRatesStore());
        var document = SeedDataGenerator.MakeTaxRatesSimple(74);
        await db.Update(document, new TaxRatesQuery { WorldId = 74 });
        await db.Update(document, new TaxRatesQuery { WorldId = 74 });

        document = SeedDataGenerator.MakeTaxRatesSimple(74);
        await db.Update(document, new TaxRatesQuery { WorldId = 74 });
    }

    [Fact]
    public async Task Update_DoesUpdate()
    {
        const uint worldId = 74;

        var db = new TaxRatesDbAccess(new MockTaxRatesStore());
        var query = new TaxRatesQuery { WorldId = worldId };

        var document1 = SeedDataGenerator.MakeTaxRatesSimple(worldId);
        await db.Update(document1, query);

        var document2 = SeedDataGenerator.MakeTaxRatesSimple(worldId);
        await db.Update(document2, query);

        var retrieved = await db.Retrieve(query);
        Assert.Equal(document2, retrieved);
    }
}