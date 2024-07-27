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
}
