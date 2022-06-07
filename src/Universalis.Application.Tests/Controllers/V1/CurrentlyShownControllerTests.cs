using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Caching;
using Universalis.Application.Controllers;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1;
using Universalis.DataTransformations;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Tests;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

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
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        const uint itemId = 5333;
        var document = SeedDataGenerator.MakeCurrentlyShownSimple(74, itemId);
        await dbAccess.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var result = await controller.Get(itemId.ToString(), worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        AssertCurrentlyShownValidWorld(document, currentlyShown, gameData);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_ReturnsOne_WithListings(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        const uint itemId = 5333;
        var document = SeedDataGenerator.MakeCurrentlyShownSimple(74, itemId);
        await dbAccess.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var result = await controller.Get(itemId.ToString(), worldOrDc, "1");
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(currentlyShown);
        Assert.Single(currentlyShown.Listings);

        Assert.True(currentlyShown.RecentHistory.Count > 1);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_ReturnsOne_WithEntries(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        const uint itemId = 5333;
        var document = SeedDataGenerator.MakeCurrentlyShownSimple(74, itemId);
        await dbAccess.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var result = await controller.Get(itemId.ToString(), worldOrDc, entriesToReturn: "1");
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(currentlyShown);
        Assert.Single(currentlyShown.RecentHistory);

        Assert.True(currentlyShown.Listings.Count > 1);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_MultiItem_World(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        var document1 = SeedDataGenerator.MakeCurrentlyShownSimple(74, 5333);
        await dbAccess.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShownSimple(74, 5);
        await dbAccess.Update(document2, new CurrentlyShownQuery { WorldId = 74, ItemId = 5 });

        var result = await controller.Get("5, 5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Empty(currentlyShown.UnresolvedItemIds);
        Assert.Equal(2, currentlyShown.ItemIds.Count);
        Assert.Equal(2, currentlyShown.Items.Count);
        Assert.Equal(document1.WorldId, currentlyShown.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[document1.WorldId], currentlyShown.WorldName);
        Assert.Null(currentlyShown.DcName);

        AssertCurrentlyShownValidWorld(document1, currentlyShown.Items.First(item => item.ItemId == document1.ItemId), gameData);
        AssertCurrentlyShownValidWorld(document2, currentlyShown.Items.First(item => item.ItemId == document2.ItemId), gameData);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter(string worldOrDc)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShownSimple(74, 5333, unixNowMs);
        await dbAccess.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShownSimple(34, 5333, unixNowMs);
        await dbAccess.Update(document2, new CurrentlyShownQuery { WorldId = 34, ItemId = 5333 });

        var result = await controller.Get("5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        var joinedListings = document1.Listings.Concat(document2.Listings).ToList();
        var joinedSales = document1.Sales.Concat(document2.Sales).ToList();
        var joinedDocument = new CurrentlyShownSimple(0, 5333, unixNowMs, "test runner", joinedListings, joinedSales);

        AssertCurrentlyShownDataCenter(
            joinedDocument,
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
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShownSimple(74, 5333, unixNowMs);
        await dbAccess.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShownSimple(34, 5, unixNowMs);
        await dbAccess.Update(document2, new CurrentlyShownQuery { WorldId = 34, ItemId = 5 });

        var result = await controller.Get("5,5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, currentlyShown.ItemIds);
        Assert.Contains(5333U, currentlyShown.ItemIds);
        Assert.Empty(currentlyShown.UnresolvedItemIds);
        Assert.Equal(2, currentlyShown.Items.Count);
        Assert.Null(currentlyShown.WorldId);
        Assert.Null(currentlyShown.WorldName);
        Assert.Equal("Crystal", currentlyShown.DcName);

        AssertCurrentlyShownDataCenter(
            document1,
            currentlyShown.Items.First(item => item.ItemId == document1.ItemId),
            unixNowMs,
            worldOrDc,
            unixNowMs);
        AssertCurrentlyShownDataCenter(
            document2,
            currentlyShown.Items.First(item => item.ItemId == document2.ItemId),
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
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

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
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        var result = await controller.Get("5333,5", worldOrDc);

        var history = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

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
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

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
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        var result = await controller.Get("5333,5", worldOrDc);

        var history = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, history.UnresolvedItemIds);
        Assert.Contains(5333U, history.UnresolvedItemIds);
        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal("Crystal", history.DcName);
        Assert.Null(history.WorldId);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_SingleItem_World_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        const uint itemId = 0;
        var result = await controller.Get(itemId.ToString(), "74");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_World_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        var result = await controller.Get("0, 4294967295", "74");

        var history = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

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
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        const uint itemId = 0;
        var result = await controller.Get(itemId.ToString(), "Crystal");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockCurrentlyShownDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var controller = new CurrentlyShownController(gameData, dbAccess, cache);

        var result = await controller.Get("0 ,4294967295", "crystal");

        var history = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0U, history.UnresolvedItemIds);
        Assert.Contains(4294967295U, history.UnresolvedItemIds);
        Assert.Contains(0U, history.ItemIds);
        Assert.Contains(4294967295U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal("Crystal", history.DcName);
        Assert.Null(history.WorldId);
    }

    private static void AssertCurrentlyShownValidWorld(CurrentlyShownSimple document, CurrentlyShownView currentlyShown, IGameDataProvider gameData)
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
        document.Sales.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

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

        var nqHistory = document.Sales.Where(s => !s.Hq).ToList();
        var hqHistory = document.Sales.Where(s => s.Hq).ToList();

        var currentAveragePrice = Filters.RemoveOutliers(document.Listings.Select(s => (float)s.PricePerUnit), 3).Average();
        var currentAveragePriceNq = Filters.RemoveOutliers(nqListings.Select(s => (float)s.PricePerUnit), 3).Average();
        var currentAveragePriceHq = Filters.RemoveOutliers(hqListings.Select(s => (float)s.PricePerUnit), 3).Average();

        Assert.Equal(Round(currentAveragePrice), Round(currentlyShown.CurrentAveragePrice));
        Assert.Equal(Round(currentAveragePriceNq), Round(currentlyShown.CurrentAveragePriceNq));
        Assert.Equal(Round(currentAveragePriceHq), Round(currentlyShown.CurrentAveragePriceHq));

        var averagePrice = Filters.RemoveOutliers(document.Sales.Select(s => (float)s.PricePerUnit), 3).Average();
        var averagePriceNq = Filters.RemoveOutliers(nqHistory.Select(s => (float)s.PricePerUnit), 3).Average();
        var averagePriceHq = Filters.RemoveOutliers(hqHistory.Select(s => (float)s.PricePerUnit), 3).Average();

        Assert.Equal(Round(averagePrice), Round(currentlyShown.AveragePrice));
        Assert.Equal(Round(averagePriceNq), Round(currentlyShown.AveragePriceNq));
        Assert.Equal(Round(averagePriceHq), Round(currentlyShown.AveragePriceHq));

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

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var saleVelocity = Statistics.VelocityPerDay(
            currentlyShown.RecentHistory.Select(s => s.TimestampUnixSeconds * 1000), now, WeekLength);
        var saleVelocityNq = Statistics.VelocityPerDay(
            nqHistory.Select(s => s.TimestampUnixSeconds * 1000), now, WeekLength);
        var saleVelocityHq = Statistics.VelocityPerDay(
            hqHistory.Select(s => s.TimestampUnixSeconds * 1000), now, WeekLength);

        Assert.Equal(Round(saleVelocity), Round(currentlyShown.SaleVelocity));
        Assert.Equal(Round(saleVelocityNq), Round(currentlyShown.SaleVelocityNq));
        Assert.Equal(Round(saleVelocityHq), Round(currentlyShown.SaleVelocityHq));

        var stackSizeHistogram = new SortedDictionary<int, int>(Statistics.GetDistribution(listings.Select(l => (int)l.Quantity)));
        var stackSizeHistogramNq = new SortedDictionary<int, int>(Statistics.GetDistribution(nqListings.Select(l => (int)l.Quantity)));
        var stackSizeHistogramHq = new SortedDictionary<int, int>(Statistics.GetDistribution(hqListings.Select(l => (int)l.Quantity)));

        Assert.Equal(stackSizeHistogram, currentlyShown.StackSizeHistogram);
        Assert.Equal(stackSizeHistogramNq, currentlyShown.StackSizeHistogramNq);
        Assert.Equal(stackSizeHistogramHq, currentlyShown.StackSizeHistogramHq);
    }

    private static void AssertCurrentlyShownDataCenter(CurrentlyShownSimple anyWorldDocument, CurrentlyShownView currentlyShown, long lastUploadTime, string worldOrDc, long unixNowMs)
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
        anyWorldDocument.Sales.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

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

        var nqHistory = anyWorldDocument.Sales.Where(s => !s.Hq).ToList();
        var hqHistory = anyWorldDocument.Sales.Where(s => s.Hq).ToList();

        var currentAveragePrice = Filters.RemoveOutliers(listings.Select(s => (float)s.PricePerUnit), 3).Average();
        var currentAveragePriceNq = Filters.RemoveOutliers(nqListings.Select(s => (float)s.PricePerUnit), 3).Average();
        var currentAveragePriceHq = Filters.RemoveOutliers(hqListings.Select(s => (float)s.PricePerUnit), 3).Average();

        Assert.Equal(Round(currentAveragePrice), Round(currentlyShown.CurrentAveragePrice));
        Assert.Equal(Round(currentAveragePriceNq), Round(currentlyShown.CurrentAveragePriceNq));
        Assert.Equal(Round(currentAveragePriceHq), Round(currentlyShown.CurrentAveragePriceHq));

        var averagePrice = Filters.RemoveOutliers(anyWorldDocument.Sales.Select(s => (float)s.PricePerUnit), 3).Average();
        var averagePriceNq = Filters.RemoveOutliers(nqHistory.Select(s => (float)s.PricePerUnit), 3).Average();
        var averagePriceHq = Filters.RemoveOutliers(hqHistory.Select(s => (float)s.PricePerUnit), 3).Average();

        Assert.Equal(Round(averagePrice), Round(currentlyShown.AveragePrice));
        Assert.Equal(Round(averagePriceNq), Round(currentlyShown.AveragePriceNq));
        Assert.Equal(Round(averagePriceHq), Round(currentlyShown.AveragePriceHq));

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

        var saleVelocity = Statistics.VelocityPerDay(
            currentlyShown.RecentHistory.Select(s => s.TimestampUnixSeconds * 1000), unixNowMs, WeekLength);
        var saleVelocityNq = Statistics.VelocityPerDay(
            nqHistory.Select(s => (long)s.TimestampUnixSeconds * 1000), unixNowMs, WeekLength);
        var saleVelocityHq = Statistics.VelocityPerDay(
            hqHistory.Select(s => (long)s.TimestampUnixSeconds * 1000), unixNowMs, WeekLength);

        Assert.Equal(Round(saleVelocity), Round(currentlyShown.SaleVelocity));
        Assert.Equal(Round(saleVelocityNq), Round(currentlyShown.SaleVelocityNq));
        Assert.Equal(Round(saleVelocityHq), Round(currentlyShown.SaleVelocityHq));

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