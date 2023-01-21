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

        await store.Insert(sale);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Null_OnMannequin_DoesNotWork()
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
            OnMannequin = null,
            SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => store.Insert(sale));
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Null_DoesNotWork()
    {
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.Insert(null));
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Null_BuyerName_DoesNotWork()
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
            BuyerName = null,
            OnMannequin = false,
            SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => store.Insert(sale));
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Null_Quantity_DoesNotWork()
    {
        var store = _fixture.Services.GetRequiredService<ISaleStore>();
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            WorldId = 23,
            ItemId = 5333,
            Hq = true,
            PricePerUnit = 300,
            Quantity = null,
            BuyerName = "Hello World",
            OnMannequin = false,
            SaleTime = new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            UploaderIdHash = "efuwhafejgj3weg0wrkporeh",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => store.Insert(sale));
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

        await store.Insert(sale);
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
}
