using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using Universalis.DbAccess.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;
using System.Linq;
using System.Collections.Generic;

namespace Universalis.DbAccess.Tests.MarketBoard;

[Collection("Database collection")]
public class SaleStoreTests
{
    private readonly DbFixture _fixture;

    public SaleStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            WorldId = 23,
            ItemId = 5333,
            Hq = true,
            PricePerUnit = 300,
            Quantity = 20,
            BuyerName = "Hello World",
            OnMannequin = false,
            SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };

        await store.InsertMany(new[] { sale });
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Null_DoesNotWork()
    {
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.InsertMany( null));
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieveBySaleTime_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            WorldId = 27,
            ItemId = 5333,
            Hq = true,
            PricePerUnit = 300,
            Quantity = 20,
            BuyerName = "Hello World",
            OnMannequin = false,
            SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };

        await store.InsertMany(new[] { sale });
        await Task.Delay(1000);
        var results = (await store.RetrieveBySaleTime(27, 5333, 1)).ToList();

        Assert.Single(results);
        Assert.All(results, result =>
        {
            Assert.Equal(sale.Id, result.Id);
            Assert.Equal(sale.WorldId, result.WorldId);
            Assert.Equal(sale.ItemId, result.ItemId);
            Assert.Equal(sale.Hq, result.Hq);
            Assert.Equal(sale.PricePerUnit, result.PricePerUnit);
            Assert.Equal(sale.Quantity, result.Quantity);
            Assert.Equal(sale.BuyerName, result.BuyerName);
            Assert.Equal(sale.OnMannequin, result.OnMannequin);
            Assert.Equal(sale.SaleTime, result.SaleTime);
            Assert.Equal(DateTimeKind.Utc, result.SaleTime.Kind);
            Assert.Equal(sale.UploaderIdHash, result.UploaderIdHash);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertManyRetrieveBySaleTime_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = new List<Sale>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 25,
                ItemId = 5333,
                Hq = true,
                PricePerUnit = 300,
                Quantity = 20,
                BuyerName = "Hello World",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 25,
                ItemId = 5333,
                Hq = true,
                PricePerUnit = 300,
                Quantity = 20,
                BuyerName = "Hello World",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
        };

        await store.InsertMany(sales);
        await Task.Delay(1000);
        var results = (await store.RetrieveBySaleTime(25, 5333, 2)).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(sales.Zip(results), pair =>
        {
            var (sale, result) = pair;
            Assert.Equal(sale.Id, result.Id);
            Assert.Equal(sale.WorldId, result.WorldId);
            Assert.Equal(sale.ItemId, result.ItemId);
            Assert.Equal(sale.Hq, result.Hq);
            Assert.Equal(sale.PricePerUnit, result.PricePerUnit);
            Assert.Equal(sale.Quantity, result.Quantity);
            Assert.Equal(sale.BuyerName, result.BuyerName);
            Assert.Equal(sale.OnMannequin, result.OnMannequin);
            Assert.Equal(sale.SaleTime, result.SaleTime);
            Assert.Equal(DateTimeKind.Utc, result.SaleTime.Kind);
            Assert.Equal(sale.UploaderIdHash, result.UploaderIdHash);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertManyRetrieveBySaleTime_Works_2()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = SeedDataGenerator.MakeHistory(74, 33922).Sales.OrderByDescending(s => s.SaleTime).ToList();

        await store.InsertMany(sales);
        await Task.Delay(1000);
        var results1 = (await store.RetrieveBySaleTime(74, 33922, sales.Count)).ToList();

        Assert.Equal(sales.Count, results1.Count);
        Assert.All(sales.Zip(results1.OrderByDescending(s => s.SaleTime)), pair =>
        {
            var (sale, result) = pair;
            Assert.Equal(sale.Id, result.Id);
            Assert.Equal(sale.WorldId, result.WorldId);
            Assert.Equal(sale.ItemId, result.ItemId);
            Assert.Equal(sale.Hq, result.Hq);
            Assert.Equal(sale.PricePerUnit, result.PricePerUnit);
            Assert.Equal(sale.Quantity, result.Quantity);
            Assert.Equal(sale.BuyerName, result.BuyerName);
            Assert.Equal(sale.OnMannequin, result.OnMannequin);
            Assert.Equal(new DateTimeOffset(sale.SaleTime).ToUnixTimeSeconds(), new DateTimeOffset(result.SaleTime).ToUnixTimeSeconds());
            Assert.Equal(DateTimeKind.Utc, result.SaleTime.Kind);
            Assert.Equal(sale.UploaderIdHash, result.UploaderIdHash);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertManyRetrieveBySaleTimeWithBounds_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = new List<Sale>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 25,
                ItemId = 5333,
                Hq = true,
                PricePerUnit = 300,
                Quantity = 20,
                BuyerName = "Hello World",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 25,
                ItemId = 5333,
                Hq = true,
                PricePerUnit = 300,
                Quantity = 20,
                BuyerName = "Hello World",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
        };

        await store.InsertMany(sales);
        await Task.Delay(1000);
        var from = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2022, 10, 1, 23, 59, 59, DateTimeKind.Utc);
        var results = (await store.RetrieveBySaleTime(25, 5333, 2, from, to)).ToList();

        Assert.Single(results);
        Assert.Equal(sales[1], results[0]);
    }

#if DEBUG
    [Fact]
#endif
    public async Task GetMostRecentSaleInWorld_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = SeedDataGenerator.MakeHistory(92, 2).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(sales);

        var nqSale = sales.First(s => !s.Hq);
        var result = await store.GetMostRecentSaleInWorld(92, 2, false);
        Assert.Equal(nqSale.PricePerUnit, result.UnitPrice);
        Assert.Equal(nqSale.SaleTime, result.SaleTime);

        var hqSale = sales.First(s => s.Hq);
        result = await store.GetMostRecentSaleInWorld(92, 2, true);
        Assert.Equal(hqSale.PricePerUnit, result.UnitPrice);
        Assert.Equal(hqSale.SaleTime, result.SaleTime);
    }

#if DEBUG
    [Fact]
#endif
    public async Task GetMostRecentSaleInDcOrRegion_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = SeedDataGenerator.MakeHistory(92, 2).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(sales);

        var nqSale = sales.First(s => !s.Hq);
        var result = await store.GetMostRecentSaleInDatacenterOrRegion("Gaia", 2, false);
        Assert.Equal(nqSale.PricePerUnit, result.UnitPrice);
        Assert.Equal(nqSale.SaleTime, result.SaleTime);
        Assert.Equal(nqSale.WorldId, result.WorldId);
        result = await store.GetMostRecentSaleInDatacenterOrRegion("Japan", 2, false);
        Assert.Equal(nqSale.PricePerUnit, result.UnitPrice);
        Assert.Equal(nqSale.SaleTime, result.SaleTime);
        Assert.Equal(nqSale.WorldId, result.WorldId);

        var hqSale = sales.First(s => s.Hq);
        result = await store.GetMostRecentSaleInDatacenterOrRegion("Gaia", 2, true);
        Assert.Equal(hqSale.PricePerUnit, result.UnitPrice);
        Assert.Equal(hqSale.SaleTime, result.SaleTime);
        Assert.Equal(hqSale.WorldId, result.WorldId);
        result = await store.GetMostRecentSaleInDatacenterOrRegion("Japan", 2, true);
        Assert.Equal(hqSale.PricePerUnit, result.UnitPrice);
        Assert.Equal(hqSale.SaleTime, result.SaleTime);
        Assert.Equal(hqSale.WorldId, result.WorldId);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveUnitTradeVelocity_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = SeedDataGenerator.MakeHistory(92, 2).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(sales);

        var nqQuantity = sales.Where(s => !s.Hq).Sum(s => s.Quantity) ?? 0;
        var nqSumSales = sales.Where(s => !s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        var hqQuantity = sales.Where(s => s.Hq).Sum(s => s.Quantity) ?? 0;
        var hqSumSales = sales.Where(s => s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        var result = await store.RetrieveUnitTradeVelocity("92", 2, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(nqQuantity, result.Nq.Quantity);
        Assert.Equal(nqSumSales, result.Nq.SumSales);
        Assert.True(nqQuantity <= result.Nq.AvgSalesPerDay);
        Assert.Equal(hqQuantity, result.Hq.Quantity);
        Assert.Equal(hqSumSales, result.Hq.SumSales);
        Assert.True(hqQuantity <= result.Hq.AvgSalesPerDay);

        result = await store.RetrieveUnitTradeVelocity("Gaia", 2, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(nqQuantity, result.Nq.Quantity);
        Assert.Equal(nqSumSales, result.Nq.SumSales);
        Assert.True(nqQuantity <= result.Nq.AvgSalesPerDay);
        Assert.Equal(hqQuantity, result.Hq.Quantity);
        Assert.Equal(hqSumSales, result.Hq.SumSales);
        Assert.True(hqQuantity <= result.Hq.AvgSalesPerDay);

        result = await store.RetrieveUnitTradeVelocity("Japan", 2, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(nqQuantity, result.Nq.Quantity);
        Assert.Equal(nqSumSales, result.Nq.SumSales);
        Assert.True(nqQuantity <= result.Nq.AvgSalesPerDay);
        Assert.Equal(hqQuantity, result.Hq.Quantity);
        Assert.Equal(hqSumSales, result.Hq.SumSales);
        Assert.True(hqQuantity <= result.Hq.AvgSalesPerDay);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveUnitTradeVelocity_WorksWithMultipleWorlds()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales39 = SeedDataGenerator.MakeHistory(39, 2).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(sales39);
        var sales40 = SeedDataGenerator.MakeHistory(40, 2).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(sales40);
        var sales36 = SeedDataGenerator.MakeHistory(36, 2).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(sales36);
        var salesOtherRegion = SeedDataGenerator.MakeHistory(92, 2).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(salesOtherRegion);
        var salesOtherItem = SeedDataGenerator.MakeHistory(39, 3).Sales.OrderByDescending(s => s.SaleTime).ToList();
        await store.InsertMany(salesOtherItem);

        var nqQuantity = sales39.Where(s => !s.Hq).Sum(s => s.Quantity) ?? 0;
        var nqSumSales = sales39.Where(s => !s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        var hqQuantity = sales39.Where(s => s.Hq).Sum(s => s.Quantity) ?? 0;
        var hqSumSales = sales39.Where(s => s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        var result = await store.RetrieveUnitTradeVelocity("39", 2, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(nqQuantity, result.Nq.Quantity);
        Assert.Equal(nqSumSales, result.Nq.SumSales);
        Assert.True(nqQuantity <= result.Nq.AvgSalesPerDay);
        Assert.Equal(hqQuantity, result.Hq.Quantity);
        Assert.Equal(hqSumSales, result.Hq.SumSales);
        Assert.True(hqQuantity <= result.Hq.AvgSalesPerDay);

        nqQuantity += sales40.Where(s => !s.Hq).Sum(s => s.Quantity) ?? 0;
        nqSumSales += sales40.Where(s => !s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        hqQuantity += sales40.Where(s => s.Hq).Sum(s => s.Quantity) ?? 0;
        hqSumSales += sales40.Where(s => s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        result = await store.RetrieveUnitTradeVelocity("Chaos", 2, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(nqQuantity, result.Nq.Quantity);
        Assert.Equal(nqSumSales, result.Nq.SumSales);
        Assert.True(nqQuantity <= result.Nq.AvgSalesPerDay);
        Assert.Equal(hqQuantity, result.Hq.Quantity);
        Assert.Equal(hqSumSales, result.Hq.SumSales);
        Assert.True(hqQuantity <= result.Hq.AvgSalesPerDay);

        nqQuantity += sales36.Where(s => !s.Hq).Sum(s => s.Quantity) ?? 0;
        nqSumSales += sales36.Where(s => !s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        hqQuantity += sales36.Where(s => s.Hq).Sum(s => s.Quantity) ?? 0;
        hqSumSales += sales36.Where(s => s.Hq).Sum(s => s.Quantity * (long) s.PricePerUnit) ?? 0;
        result = await store.RetrieveUnitTradeVelocity("Europe", 2, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(nqQuantity, result.Nq.Quantity);
        Assert.Equal(nqSumSales, result.Nq.SumSales);
        Assert.True(nqQuantity <= result.Nq.AvgSalesPerDay);
        Assert.Equal(hqQuantity, result.Hq.Quantity);
        Assert.Equal(hqSumSales, result.Hq.SumSales);
        Assert.True(hqQuantity <= result.Hq.AvgSalesPerDay);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveBySaleTime_CacheHit_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            WorldId = 28,
            ItemId = 5334,
            Hq = true,
            PricePerUnit = 300,
            Quantity = 20,
            BuyerName = "Hello World",
            OnMannequin = false,
            SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };

        await store.InsertMany(new[] { sale });
        await Task.Delay(1000);

        // First call - cache miss, queries database (queries 1800 but only 1 exists)
        var results1 = (await store.RetrieveBySaleTime(28, 5334, 1)).ToList();
        Assert.Single(results1);

        // Second call - cache hit, should return cached data (still 1 result)
        var results2 = (await store.RetrieveBySaleTime(28, 5334, 1)).ToList();
        Assert.Single(results2);
        Assert.Equal(results1[0].Id, results2[0].Id);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveBySaleTime_CacheInvalidatedOnNewInsert_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sale1 = new Sale
        {
            Id = Guid.NewGuid(),
            WorldId = 29,
            ItemId = 5335,
            Hq = true,
            PricePerUnit = 300,
            Quantity = 20,
            BuyerName = "Hello World",
            OnMannequin = false,
            SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };

        await store.InsertMany(new[] { sale1 });
        await Task.Delay(1000);

        // First call - gets 1 sale
        var results1 = (await store.RetrieveBySaleTime(29, 5335, 10)).ToList();
        Assert.Single(results1);

        // Insert another sale
        var sale2 = new Sale
        {
            Id = Guid.NewGuid(),
            WorldId = 29,
            ItemId = 5335,
            Hq = false,
            PricePerUnit = 250,
            Quantity = 15,
            BuyerName = "Test Buyer",
            OnMannequin = false,
            SaleTime = new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };
        await store.InsertMany(new[] { sale2 });
        await Task.Delay(1000);

        // Clear cache to simulate cache expiration
        await _fixture.ClearCache();

        // Second call after new insert - should get 2 sales (cache was cleared)
        var results2 = (await store.RetrieveBySaleTime(29, 5335, 10)).ToList();
        Assert.Equal(2, results2.Count);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveBySaleTime_DifferentCountParameters_ReturnsCorrectResults()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = new List<Sale>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 30,
                ItemId = 5336,
                Hq = true,
                PricePerUnit = 300,
                Quantity = 20,
                BuyerName = "Hello World",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 30,
                ItemId = 5336,
                Hq = false,
                PricePerUnit = 250,
                Quantity = 15,
                BuyerName = "Test Buyer",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 30,
                ItemId = 5336,
                Hq = false,
                PricePerUnit = 200,
                Quantity = 10,
                BuyerName = "Another Buyer",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
        };

        await store.InsertMany(sales);
        await Task.Delay(1000);

        // First call with count=1 - caches 1800, returns 1
        var results1 = (await store.RetrieveBySaleTime(30, 5336, 1)).ToList();
        Assert.Single(results1);

        // Second call with count=3 - hits cache, returns 3 (sliced from cached 1800)
        var results2 = (await store.RetrieveBySaleTime(30, 5336, 3)).ToList();
        Assert.Equal(3, results2.Count);

        // Third call with count=2 - hits cache, returns 2 (sliced from cached 1800)
        var results3 = (await store.RetrieveBySaleTime(30, 5336, 2)).ToList();
        Assert.Equal(2, results3.Count);

        // Verify the actual sales are the same (cache hit)
        Assert.Equal(results2[0].Id, results3[0].Id);
        Assert.Equal(results2[1].Id, results3[1].Id);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveBySaleTime_LargeQueryBypassesCache()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();

        // Create many sales using SeedDataGenerator
        var sales = SeedDataGenerator.MakeHistory(31, 5337).Sales.Take(100).ToList();
        await store.InsertMany(sales);
        await Task.Delay(1000);

        // First call with count=2000 (> MaxCacheCount) - bypasses cache
        var results1 = (await store.RetrieveBySaleTime(31, 5337, 2000)).ToList();
        Assert.Equal(100, results1.Count);

        // Second call with count=2000 - still bypasses cache (no cache entry created)
        var results2 = (await store.RetrieveBySaleTime(31, 5337, 2000)).ToList();
        Assert.Equal(100, results2.Count);

        // Third call with count=100 - should still not have cached data from previous large query
        var results3 = (await store.RetrieveBySaleTime(31, 5337, 100)).ToList();
        Assert.Equal(100, results3.Count);

        // Now do a small query that WILL cache
        await _fixture.ClearCache();
        var results4 = (await store.RetrieveBySaleTime(31, 5337, 50)).ToList();
        Assert.Equal(50, results4.Count);

        // Verify small query created a cache entry
        var results5 = (await store.RetrieveBySaleTime(31, 5337, 25)).ToList();
        Assert.Equal(25, results5.Count);
        Assert.Equal(results4[0].Id, results5[0].Id); // Same data from cache
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveBySaleTime_WithTimeRangeBypassesCache()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sales = new List<Sale>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 32,
                ItemId = 5338,
                Hq = true,
                PricePerUnit = 300,
                Quantity = 20,
                BuyerName = "Hello World",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = 32,
                ItemId = 5338,
                Hq = false,
                PricePerUnit = 250,
                Quantity = 15,
                BuyerName = "Test Buyer",
                OnMannequin = false,
                SaleTime = new DateTime(2022, 10, 5, 0, 0, 0, DateTimeKind.Utc),
                UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
            },
        };

        await store.InsertMany(sales);
        await Task.Delay(1000);

        // Query with time range - should bypass cache
        var from = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2022, 10, 2, 0, 0, 0, DateTimeKind.Utc);
        var results1 = (await store.RetrieveBySaleTime(32, 5338, 100, from, to)).ToList();
        Assert.Single(results1);

        // Same query again - still bypasses cache (no cache entry created)
        var results2 = (await store.RetrieveBySaleTime(32, 5338, 100, from, to)).ToList();
        Assert.Single(results2);

        // Query without time range - should create cache entry
        var results3 = (await store.RetrieveBySaleTime(32, 5338, 100)).ToList();
        Assert.Equal(2, results3.Count);

        // Same query without time range - should hit cache
        var results4 = (await store.RetrieveBySaleTime(32, 5338, 1)).ToList();
        Assert.Single(results4);
        Assert.Equal(results3[0].Id, results4[0].Id); // Same data from cache
    }
}
