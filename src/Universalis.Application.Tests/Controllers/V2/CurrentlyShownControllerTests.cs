using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Caching;
using Universalis.Application.Controllers;
using Universalis.Application.Controllers.V2;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1;
using Universalis.Application.Views.V2;
using Universalis.DataTransformations;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Tests;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V2;

public class CurrentlyShownControllerTests
{
    private const long WeekLength = 604800000L;

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_SingleItem_World(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        const uint itemId = 5333;
        var document = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
        await currentlyShownDb.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = itemId });
        
        var sales = SeedDataGenerator.MakeHistory(74, itemId).Sales;
        await historyDb.InsertSales(sales, new HistoryQuery { WorldId = 74, ItemId = itemId });

        var result = await controller.Get(itemId.ToString(), worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        AssertCurrentlyShownValidWorld(document, sales, currentlyShown, gameData, now);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_MultiItem_World(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);
        
        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
        await currentlyShownDb.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333).Sales;
        await historyDb.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShown(74, 5);
        await currentlyShownDb.Update(document2, new CurrentlyShownQuery { WorldId = 74, ItemId = 5 });
        
        var sales2 = SeedDataGenerator.MakeHistory(74, 5).Sales;
        await historyDb.InsertSales(sales2, new HistoryQuery { WorldId = 74, ItemId = 5 });

        var result = await controller.Get("5, 5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(currentlyShown);
        Assert.Empty(currentlyShown.UnresolvedItemIds);
        Assert.Equal(2, currentlyShown.ItemIds.Count);
        Assert.Equal(2, currentlyShown.Items.Count);
        Assert.Equal(document1.WorldId, currentlyShown.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[document1.WorldId], currentlyShown.WorldName);
        Assert.Null(currentlyShown.DcName);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        AssertCurrentlyShownValidWorld(document1, sales1, currentlyShown.Items.First(item => item.Key == document1.ItemId).Value, gameData, now);
        AssertCurrentlyShownValidWorld(document2, sales2, currentlyShown.Items.First(item => item.Key == document2.ItemId).Value, gameData, now);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);
        var unixNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333, unixNowMs);
        await currentlyShownDb.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs).Sales;
        await historyDb.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShown(34, 5333, unixNowMs);
        await currentlyShownDb.Update(document2, new CurrentlyShownQuery { WorldId = 34, ItemId = 5333 });
        
        var sales2 = SeedDataGenerator.MakeHistory(34, 5333, unixNowMs).Sales;
        await historyDb.InsertSales(sales2, new HistoryQuery { WorldId = 34, ItemId = 5333 });

        var result = await controller.Get("5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        var joinedListings = document1.Listings.Concat(document2.Listings).ToList();
        var joinedSales = sales1.Concat(sales2).ToList();
        var joinedDocument = new CurrentlyShown(0, 5333, unixNowMs, "test runner", joinedListings);

        AssertCurrentlyShownDataCenter(
            joinedDocument,
            joinedSales,
            currentlyShown,
            unixNowMs,
            worldOrDc,
            unixNowMs);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333, unixNowMs);
        await currentlyShownDb.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs).Sales;
        await historyDb.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShown(34, 5, unixNowMs);
        await currentlyShownDb.Update(document2, new CurrentlyShownQuery { WorldId = 34, ItemId = 5 });
        
        var sales2 = SeedDataGenerator.MakeHistory(34, 5, unixNowMs).Sales;
        await historyDb.InsertSales(sales2, new HistoryQuery { WorldId = 34, ItemId = 5 });

        var result = await controller.Get("5,5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(currentlyShown);
        Assert.Contains(5U, currentlyShown.ItemIds);
        Assert.Contains(5333U, currentlyShown.ItemIds);
        Assert.Empty(currentlyShown.UnresolvedItemIds);
        Assert.Equal(2, currentlyShown.Items.Count);
        Assert.Null(currentlyShown.WorldId);
        Assert.Null(currentlyShown.WorldName);
        Assert.Equal("Crystal", currentlyShown.DcName);

        AssertCurrentlyShownDataCenter(
            document1,
            sales1,
            currentlyShown.Items.First(item => item.Key == document1.ItemId).Value,
            unixNowMs,
            worldOrDc,
            unixNowMs);
        AssertCurrentlyShownDataCenter(
            document2,
            sales2,
            currentlyShown.Items.First(item => item.Key == document2.ItemId).Value,
            unixNowMs,
            worldOrDc,
            unixNowMs);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_SingleItem_World_WhenNone(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        const uint itemId = 5333;
        var result = await controller.Get(itemId.ToString(), worldOrDc);

        var history = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(itemId, history.ItemId);
        Assert.Equal(74U, history.WorldId);
        Assert.Equal("Coeurl", history.WorldName);
        Assert.Null(history.DcName);
        Assert.NotNull(history.Listings);
        Assert.Empty(history.Listings);
        Assert.NotNull(history.RecentHistory);
        Assert.Empty(history.RecentHistory);
        Assert.Equal(0U, history.LastUploadTimeUnixMilliseconds);
        Assert.NotNull(history.StackSizeHistogram);
        Assert.Empty(history.StackSizeHistogram);
        Assert.NotNull(history.StackSizeHistogramNq);
        Assert.Empty(history.StackSizeHistogramNq);
        Assert.NotNull(history.StackSizeHistogramHq);
        Assert.Empty(history.StackSizeHistogramHq);
        Assert.Equal(0, history.SaleVelocity);
        Assert.Equal(0, history.SaleVelocityNq);
        Assert.Equal(0, history.SaleVelocityHq);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_MultiItem_World_WhenNone(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        var result = await controller.Get("5333,5", worldOrDc);

        var history = (CurrentlyShownMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, history.UnresolvedItemIds);
        Assert.Contains(5333U, history.UnresolvedItemIds);
        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal(74U, history.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[74], history.WorldName);
        Assert.Null(history.DcName);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNone(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        const uint itemId = 5333;
        var result = await controller.Get(itemId.ToString(), worldOrDc);

        var history = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(itemId, history.ItemId);
        Assert.Equal("Crystal", history.DcName);
        Assert.NotNull(history.Listings);
        Assert.Empty(history.Listings);
        Assert.NotNull(history.RecentHistory);
        Assert.Empty(history.RecentHistory);
        Assert.Equal(0U, history.LastUploadTimeUnixMilliseconds);
        Assert.NotNull(history.StackSizeHistogram);
        Assert.Empty(history.StackSizeHistogram);
        Assert.NotNull(history.StackSizeHistogramNq);
        Assert.Empty(history.StackSizeHistogramNq);
        Assert.NotNull(history.StackSizeHistogramHq);
        Assert.Empty(history.StackSizeHistogramHq);
        Assert.Equal(0, history.SaleVelocity);
        Assert.Equal(0, history.SaleVelocityNq);
        Assert.Equal(0, history.SaleVelocityHq);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNone(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        var result = await controller.Get("5333,5", worldOrDc);

        var data = (CurrentlyShownMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, data.UnresolvedItemIds);
        Assert.Contains(5333U, data.UnresolvedItemIds);
        Assert.Contains(5U, data.ItemIds);
        Assert.Contains(5333U, data.ItemIds);
        Assert.Empty(data.Items);
        Assert.Equal("Crystal", data.DcName);
        Assert.Null(data.WorldId);
        Assert.Null(data.WorldName);
        Assert.All(data.Items, item => Assert.NotNull(item.Value.Listings));
        Assert.All(data.Items, item => Assert.NotNull(item.Value.RecentHistory));
    }

    [Fact]
    public async Task Controller_Get_Succeeds_SingleItem_World_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        const uint itemId = 0;
        var result = await controller.Get(itemId.ToString(), "74");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_World_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        var result = await controller.Get("0, 4294967295", "74");

        var history = (CurrentlyShownMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0U, history.UnresolvedItemIds);
        Assert.Contains(4294967295U, history.UnresolvedItemIds);
        Assert.Empty(history.Items);
        Assert.Equal(74U, history.WorldId);
        Assert.Null(history.DcName);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        const uint itemId = 0;
        var result = await controller.Get(itemId.ToString(), "Crystal");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb, cache);

        var result = await controller.Get("0 ,4294967295", "crystal");

        var history = (CurrentlyShownMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0U, history.UnresolvedItemIds);
        Assert.Contains(4294967295U, history.UnresolvedItemIds);
        Assert.Contains(0U, history.ItemIds);
        Assert.Contains(4294967295U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal("Crystal", history.DcName);
        Assert.Null(history.WorldId);
    }

    private static void AssertCurrentlyShownValidWorld(CurrentlyShown document, List<Sale> sales, CurrentlyShownView currentlyShown, IGameDataProvider gameData, long unixNowMs)
    {
        Assert.Equal(document.ItemId, currentlyShown.ItemId);
        Assert.Equal(document.WorldId, currentlyShown.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[document.WorldId], currentlyShown.WorldName);
        Assert.Equal(document.LastUploadTimeUnixMilliseconds, currentlyShown.LastUploadTimeUnixMilliseconds);
        Assert.Null(currentlyShown.DcName);

        Assert.NotNull(currentlyShown.Listings);
        Assert.NotNull(currentlyShown.RecentHistory);

        currentlyShown.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);
        document.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        sales.Sort((a, b) => (int)(b.SaleTime - a.SaleTime).TotalMilliseconds);

        var listings = document.Listings.Select(l =>
        {
            l.PricePerUnit = (uint)Math.Ceiling(l.PricePerUnit * 1.05);
            return l;
        }).ToList();

        Assert.All(currentlyShown.Listings.Select(l => (object)l.WorldId), Assert.Null);
        Assert.All(currentlyShown.Listings.Select(l => l.WorldName), Assert.Null);

        Assert.All(currentlyShown.RecentHistory.Select(s => (object)s.WorldId), Assert.Null);
        Assert.All(currentlyShown.RecentHistory.Select(s => s.WorldName), Assert.Null);

        var nqListings = listings.Where(s => !s.Hq).ToList();
        var hqListings = listings.Where(s => s.Hq).ToList();

        var nqHistory = sales.Where(s => !s.Hq).ToList();
        var hqHistory = sales.Where(s => s.Hq).ToList();

        Assert.True(currentlyShown.CurrentAveragePrice > 0);
        Assert.True(currentlyShown.CurrentAveragePriceNq > 0);
        Assert.True(currentlyShown.CurrentAveragePriceHq > 0);

        Assert.True(currentlyShown.AveragePrice > 0);
        Assert.True(currentlyShown.AveragePriceNq > 0);
        Assert.True(currentlyShown.AveragePriceHq > 0);

        var minPrice = currentlyShown.Listings.Min(l => l.PricePerUnit);
        var minPriceNq = nqListings.Min(l => l.PricePerUnit);
        var minPriceHq = hqListings.Min(l => l.PricePerUnit);

        Assert.Equal(minPrice, currentlyShown.MinPrice);
        Assert.Equal(minPriceNq, currentlyShown.MinPriceNq);
        Assert.Equal(minPriceHq, currentlyShown.MinPriceHq);

        var maxPrice = currentlyShown.Listings.Max(l => l.PricePerUnit);
        var maxPriceNq = nqListings.Max(l => l.PricePerUnit);
        var maxPriceHq = hqListings.Max(l => l.PricePerUnit);

        Assert.Equal(maxPrice, currentlyShown.MaxPrice);
        Assert.Equal(maxPriceNq, currentlyShown.MaxPriceNq);
        Assert.Equal(maxPriceHq, currentlyShown.MaxPriceHq);

        Assert.True(currentlyShown.SaleVelocity > 0);
        Assert.True(currentlyShown.SaleVelocityNq > 0);
        Assert.True(currentlyShown.SaleVelocityHq > 0);

        var stackSizeHistogram = new SortedDictionary<int, int>(Statistics.GetDistribution(listings.Select(l => (int)l.Quantity)));
        var stackSizeHistogramNq = new SortedDictionary<int, int>(Statistics.GetDistribution(nqListings.Select(l => (int)l.Quantity)));
        var stackSizeHistogramHq = new SortedDictionary<int, int>(Statistics.GetDistribution(hqListings.Select(l => (int)l.Quantity)));

        Assert.Equal(stackSizeHistogram, currentlyShown.StackSizeHistogram);
        Assert.Equal(stackSizeHistogramNq, currentlyShown.StackSizeHistogramNq);
        Assert.Equal(stackSizeHistogramHq, currentlyShown.StackSizeHistogramHq);
    }

    private static void AssertCurrentlyShownDataCenter(CurrentlyShown anyWorldDocument, List<Sale> sales, CurrentlyShownView currentlyShown, long lastUploadTime, string worldOrDc, long unixNowMs)
    {
        Assert.Equal(anyWorldDocument.ItemId, currentlyShown.ItemId);
        Assert.Equal(lastUploadTime, currentlyShown.LastUploadTimeUnixMilliseconds);
        Assert.Equal(char.ToUpperInvariant(worldOrDc[0]) + worldOrDc[1..].ToLowerInvariant(), currentlyShown.DcName);
        Assert.Null(currentlyShown.WorldId);
        Assert.Null(currentlyShown.WorldName);

        Assert.NotNull(currentlyShown.Listings);
        Assert.NotNull(currentlyShown.RecentHistory);

        currentlyShown.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);
        anyWorldDocument.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        sales.Sort((a, b) => (int)(b.SaleTime - a.SaleTime).TotalMilliseconds);

        var listings = anyWorldDocument.Listings.Select(l =>
        {
            l.PricePerUnit = (uint)Math.Ceiling(l.PricePerUnit * 1.05);
            return l;
        }).ToList();

        Assert.All(currentlyShown.Listings.Select(l => (object)l.WorldId), Assert.NotNull);
        Assert.All(currentlyShown.Listings.Select(l => l.WorldName), Assert.NotNull);

        Assert.All(currentlyShown.RecentHistory.Select(s => (object)s.WorldId), Assert.NotNull);
        Assert.All(currentlyShown.RecentHistory.Select(s => s.WorldName), Assert.NotNull);

        var nqListings = listings.Where(s => !s.Hq).ToList();
        var hqListings = listings.Where(s => s.Hq).ToList();

        Assert.True(currentlyShown.CurrentAveragePrice > 0);
        Assert.True(currentlyShown.CurrentAveragePriceNq > 0);
        Assert.True(currentlyShown.CurrentAveragePriceHq > 0);

        Assert.True(currentlyShown.AveragePrice > 0);
        Assert.True(currentlyShown.AveragePriceNq > 0);
        Assert.True(currentlyShown.AveragePriceHq > 0);

        var minPrice = currentlyShown.Listings.Min(l => l.PricePerUnit);
        var minPriceNq = nqListings.Min(l => l.PricePerUnit);
        var minPriceHq = hqListings.Min(l => l.PricePerUnit);

        Assert.Equal(minPrice, currentlyShown.MinPrice);
        Assert.Equal(minPriceNq, currentlyShown.MinPriceNq);
        Assert.Equal(minPriceHq, currentlyShown.MinPriceHq);

        var maxPrice = currentlyShown.Listings.Max(l => l.PricePerUnit);
        var maxPriceNq = nqListings.Max(l => l.PricePerUnit);
        var maxPriceHq = hqListings.Max(l => l.PricePerUnit);

        Assert.Equal(maxPrice, currentlyShown.MaxPrice);
        Assert.Equal(maxPriceNq, currentlyShown.MaxPriceNq);
        Assert.Equal(maxPriceHq, currentlyShown.MaxPriceHq);

        Assert.True(currentlyShown.SaleVelocity > 0);
        Assert.True(currentlyShown.SaleVelocityNq > 0);
        Assert.True(currentlyShown.SaleVelocityHq > 0);

        var stackSizeHistogram = new SortedDictionary<int, int>(Statistics.GetDistribution(listings.Select(l => (int)l.Quantity)));
        var stackSizeHistogramNq = new SortedDictionary<int, int>(Statistics.GetDistribution(nqListings.Select(l => (int)l.Quantity)));
        var stackSizeHistogramHq = new SortedDictionary<int, int>(Statistics.GetDistribution(hqListings.Select(l => (int)l.Quantity)));

        Assert.Equal(stackSizeHistogram, currentlyShown.StackSizeHistogram);
        Assert.Equal(stackSizeHistogramNq, currentlyShown.StackSizeHistogramNq);
        Assert.Equal(stackSizeHistogramHq, currentlyShown.StackSizeHistogramHq);
    }

    private static double Round(double value)
    {
        return Math.Round(value, 2);
    }
}