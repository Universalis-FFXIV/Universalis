﻿using System;
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
        private readonly Dictionary<(int, int), MarketItem> _data = new();

        public Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
        {
            _data[(marketItem.WorldId, marketItem.ItemId)] = marketItem;
            return Task.CompletedTask;
        }

        public ValueTask<MarketItem> Retrieve(MarketItemQuery query, CancellationToken cancellationToken = default)
        {
            return _data.TryGetValue((query.WorldId, query.ItemId), out var marketItem)
                ? ValueTask.FromResult(marketItem)
                : ValueTask.FromResult<MarketItem>(null);
        }

        public ValueTask<IEnumerable<MarketItem>> RetrieveMany(MarketItemManyQuery query,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(query.WorldIds.SelectMany(worldId => query.ItemIds.Select(itemId =>
            {
                _data.TryGetValue((worldId, itemId), out var data);
                return data ?? null;
            })).Where(o => o is not null));
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

        public async Task InsertMany(IEnumerable<Sale> sales, CancellationToken cancellationToken = default)
        {
            foreach (var sale in sales)
            {
                await Insert(sale, cancellationToken);
            }
        }

        public Task<IEnumerable<Sale>> RetrieveBySaleTime(int worldId, int itemId, int count, DateTime? from = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IEnumerable<Sale>)_data
                .Select(d => d.Value)
                .Where(sale =>
                    sale.WorldId == worldId && sale.ItemId == itemId && sale.SaleTime <= (from ?? DateTime.UtcNow))
                .OrderByDescending(sale => sale.SaleTime)
                .Take(count)
                .ToList());
        }

        public Task<long> RetrieveGilTradeVolume(int worldId, int itemId, DateTime from, DateTime to,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> RetrieveUnitTradeVolume(int worldId, int itemId, DateTime from, DateTime to,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
        var output = await db.RetrieveMany(new HistoryManyQuery { WorldIds = new[] { 74 }, ItemIds = new[] { 5333 } });
        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task InsertSales_Works()
    {
        var db = new HistoryDbAccess(new MockMarketItemStore(), new MockSaleStore());
        var history1 = SeedDataGenerator.MakeHistory(74, 5333);
        var history2 = SeedDataGenerator.MakeHistory(74, 5333);
        var history3 = SeedDataGenerator.MakeHistory(74, 5333);
        var query = new HistoryQuery { WorldId = history1.WorldId, ItemId = history1.ItemId };

        await db.InsertSales(history1.Sales, query);
        await db.InsertSales(history2.Sales, query);
        await db.InsertSales(history3.Sales, query);

        var retrieved = await db.Retrieve(query);

        var expectedSorted = history1.Sales.Concat(history2.Sales).Concat(history3.Sales)
            .OrderByDescending(s => s.SaleTime).Take(1000).ToList();
        var actualSorted = retrieved.Sales.OrderByDescending(s => s.SaleTime).ToList();

        Assert.Equal(expectedSorted, actualSorted);
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

        var output = (await db.RetrieveMany(new HistoryManyQuery
            { WorldIds = new[] { document.WorldId }, ItemIds = new[] { document.ItemId } }))?.ToList();

        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(document.WorldId, output[0].WorldId);
        Assert.Equal(document.ItemId, output[0].ItemId);

        var sortedExpected = document.Sales.OrderByDescending(s => s.SaleTime).ToList();
        var sortedActual = output.Select(h => h.Sales.OrderByDescending(s => s.SaleTime).ToList()).ToList();

        Assert.Equal(sortedExpected, sortedActual[0]);
    }
}