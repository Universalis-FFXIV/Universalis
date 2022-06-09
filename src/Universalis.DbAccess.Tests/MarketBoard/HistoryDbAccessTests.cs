using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

public class HistoryDbAccessTests
{
    private class MockMarketItemStore : IMarketItemStore
    {
        private readonly Dictionary<(uint, uint), MarketItem> _data = new();
        
        public Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
        {
            _data[(marketItem.WorldId, marketItem.ItemId)] = marketItem;
            return Task.CompletedTask;
        }

        public Task Update(MarketItem marketItem, CancellationToken cancellationToken = default)
        {
            if (!_data.ContainsKey((marketItem.WorldId, marketItem.ItemId)))
            {
                return Insert(marketItem, cancellationToken);
            }
            
            _data[(marketItem.WorldId, marketItem.ItemId)].LastUploadTime = marketItem.LastUploadTime;
            return Task.CompletedTask;
        }

        public Task<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default)
        {
            return _data.TryGetValue((worldId, itemId), out var marketItem)
                ? Task.FromResult(marketItem)
                : Task.FromResult<MarketItem>(null);
        }
    }

    private class MockSaleStore : ISaleStore
    {
        private readonly Dictionary<Guid, Sale> _data = new();
        
        public Task Insert(Sale sale, CancellationToken cancellationToken = default)
        {
            _data[sale.Id] = sale;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Sale>> RetrieveBySaleTime(uint worldId, uint itemId, int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IEnumerable<Sale>)_data
                .Select(d => d.Value)
                .Where(sale => sale.WorldId == worldId && sale.ItemId == itemId)
                .OrderByDescending(sale => sale.SaleTime)
                .ToList());
        }
    }

    [Fact]
    public async Task Create_DoesNotThrow()
    {
        var db = new HistoryDbAccess(new MockMarketItemStore(), new MockSaleStore());
        var document = SeedDataGenerator.MakeHistory(74, 5333);
        await db.Create(document);
    }

    [Fact]
    public async Task Retrieve_DoesNotThrow()
    {
        var db = new HistoryDbAccess(new MockMarketItemStore(), new MockSaleStore());
        var output = await db.Retrieve(new HistoryQuery { WorldId = 74, ItemId = 5333 });
        Assert.Null(output);
    }

    [Fact]
    public async Task RetrieveMany_DoesNotThrow()
    {
        var db = new HistoryDbAccess(new MockMarketItemStore(), new MockSaleStore());
        var output = await db.RetrieveMany(new HistoryManyQuery { WorldIds = new uint[] { 74 }, ItemId = 5333 });
        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task Update_DoesNotThrow()
    {
        var db = new HistoryDbAccess(new MockMarketItemStore(), new MockSaleStore());
        var document = SeedDataGenerator.MakeHistory(74, 5333);
        var query = new HistoryQuery { WorldId = document.WorldId, ItemId = document.ItemId };

        await db.Update(document, query);
        await db.Update(document, query);

        document.LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        await db.Update(document, query);

        var retrieved = await db.Retrieve(query);
        Assert.Equal(document.LastUploadTimeUnixMilliseconds, retrieved.LastUploadTimeUnixMilliseconds);
    }

    [Fact]
    public async Task Create_DoesInsert()
    {
        var db = new HistoryDbAccess(new MockMarketItemStore(), new MockSaleStore());

        var document = SeedDataGenerator.MakeHistory(74, 5333);
        await db.Create(document);

        var output = await db.Retrieve(new HistoryQuery { WorldId = document.WorldId, ItemId = document.ItemId });
        Assert.NotNull(output);
    }

    [Fact]
    public async Task RetrieveMany_ReturnsData()
    {
        var db = new HistoryDbAccess(new MockMarketItemStore(), new MockSaleStore());

        var document = SeedDataGenerator.MakeHistory(74, 5333);
        await db.Create(document);

        var output = (await db.RetrieveMany(new HistoryManyQuery { WorldIds = new[] { document.WorldId }, ItemId = document.ItemId }))?.ToList();
        
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(document.WorldId, output[0].WorldId);
        Assert.Equal(document.ItemId, output[0].ItemId);
        Assert.Equal(document.LastUploadTimeUnixMilliseconds, output[0].LastUploadTimeUnixMilliseconds);
        
        var sortedExpected = document.Sales.OrderByDescending(s => s.SaleTime).ToList();
        var sortedActual = output.Select(h => h.Sales.OrderByDescending(s => s.SaleTime).ToList()).ToList();
        
        Assert.Equal(sortedExpected, sortedActual[0]);
    }
}