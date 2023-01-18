using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Universalis.Application.Controllers;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1;
using Universalis.DataTransformations;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Tests;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

public class CurrentlyShownControllerTests
{
    private class TestResources
    {
        public IGameDataProvider GameData { get; private init; }
        public ICurrentlyShownDbAccess CurrentlyShown { get; private init; }
        public IHistoryDbAccess History { get; private init; }
        public CurrentlyShownController Controller { get; private init; }

        public static TestResources Create()
        {
            var gameData = new MockGameDataProvider();
            var currentlyShownDb = new MockCurrentlyShownDbAccess();
            var historyDb = new MockHistoryDbAccess();
            var controller = new CurrentlyShownController(gameData, currentlyShownDb, historyDb);
            return new TestResources
            {
                GameData = gameData,
                CurrentlyShown = currentlyShownDb,
                History = historyDb,
                Controller = controller,
            };
        }
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_SingleItem_World(string worldOrDc)
    {
        var test = TestResources.Create();

        const int itemId = 5333;
        var document = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
        await test.CurrentlyShown.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = itemId });
        
        var sales = SeedDataGenerator.MakeHistory(74, itemId).Sales;
        await test.History.InsertSales(sales, new HistoryQuery { WorldId = 74, ItemId = itemId });

        var result = await test.Controller.Get(itemId.ToString(), worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        AssertCurrentlyShownValidWorld(document, sales, currentlyShown, test.GameData);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_ReturnsOne_WithListings(string worldOrDc)
    {
        var test = TestResources.Create();

        const int itemId = 5333;
        var document = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
        await test.CurrentlyShown.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = itemId });
        
        var sales = SeedDataGenerator.MakeHistory(74, itemId).Sales;
        await test.History.InsertSales(sales, new HistoryQuery { WorldId = 74, ItemId = itemId });

        var result = await test.Controller.Get(itemId.ToString(), worldOrDc, "1");
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(currentlyShown);
        Assert.Single(currentlyShown.Listings);

        //Assert.True(currentlyShown.RecentHistory.Count > 1);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_ReturnsOne_WithEntries(string worldOrDc)
    {
        var test = TestResources.Create();

        const int itemId = 5333;
        var document = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
        await test.CurrentlyShown.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = itemId });
        
        var sales = SeedDataGenerator.MakeHistory(74, itemId).Sales;
        await test.History.InsertSales(sales, new HistoryQuery { WorldId = 74, ItemId = itemId });

        var result = await test.Controller.Get(itemId.ToString(), worldOrDc, entriesToReturn: "1");
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(currentlyShown);
        //Assert.Single(currentlyShown.RecentHistory);

        Assert.True(currentlyShown.Listings.Count > 1);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_MultiItem_World(string worldOrDc)
    {
        var test = TestResources.Create();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
        await test.CurrentlyShown.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333).Sales;
        await test.History.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShown(74, 5);
        await test.CurrentlyShown.Update(document2, new CurrentlyShownQuery { WorldId = 74, ItemId = 5 });
        
        var sales2 = SeedDataGenerator.MakeHistory(74, 5).Sales;
        await test.History.InsertSales(sales2, new HistoryQuery { WorldId = 74, ItemId = 5 });

        var result = await test.Controller.Get("5, 5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Empty(currentlyShown.UnresolvedItemIds);
        Assert.Equal(2, currentlyShown.ItemIds.Count);
        Assert.Equal(2, currentlyShown.Items.Count);
        Assert.Equal(document1.WorldId, currentlyShown.WorldId);
        Assert.Equal(test.GameData.AvailableWorlds()[document1.WorldId], currentlyShown.WorldName);
        Assert.Null(currentlyShown.DcName);

        AssertCurrentlyShownValidWorld(document1, sales1, currentlyShown.Items.First(item => item.ItemId == document1.ItemId), test.GameData);
        AssertCurrentlyShownValidWorld(document2, sales2, currentlyShown.Items.First(item => item.ItemId == document2.ItemId), test.GameData);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter(string worldOrDc)
    {
        var test = TestResources.Create();
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333, unixNowMs);
        await test.CurrentlyShown.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs).Sales;
        await test.History.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShown(34, 5333, unixNowMs);
        await test.CurrentlyShown.Update(document2, new CurrentlyShownQuery { WorldId = 34, ItemId = 5333 });
        
        var sales2 = SeedDataGenerator.MakeHistory(34, 5333, unixNowMs).Sales;
        await test.History.InsertSales(sales2, new HistoryQuery { WorldId = 34, ItemId = 5333 });

        var result = await test.Controller.Get("5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        var joinedListings = document1.Listings.Concat(document2.Listings).ToList();
        var joinedSales = sales1.Concat(sales2).ToList();
        var joinedDocument = new CurrentlyShown
        {
            WorldId = 0,
            ItemId = 5333,
            LastUploadTimeUnixMilliseconds = unixNowMs,
            UploadSource = "test runner",
            Listings = joinedListings,
        };

        AssertCurrentlyShownDataCenter(
            joinedDocument,
            joinedSales,
            currentlyShown,
            unixNowMs,
            worldOrDc);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter_When_CurrentlyShownStore_Fails(string worldOrDc)
    {
        var test = TestResources.Create();
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333, unixNowMs);
        await test.CurrentlyShown.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var sales1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs).Sales;
        await test.History.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var sales2 = SeedDataGenerator.MakeHistory(34, 5333, unixNowMs).Sales;
        await test.History.InsertSales(sales2, new HistoryQuery { WorldId = 34, ItemId = 5333 });

        var result = await test.Controller.Get("5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        var joinedSales = sales1.Concat(sales2).ToList();
        var joinedDocument = new CurrentlyShown
        {
            WorldId = 0,
            ItemId = 5333,
            LastUploadTimeUnixMilliseconds = unixNowMs,
            UploadSource = "test runner",
            Listings = document1.Listings,
        };

        AssertCurrentlyShownDataCenter(
            joinedDocument,
            joinedSales,
            currentlyShown,
            unixNowMs,
            worldOrDc);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter(string worldOrDc)
    {
        var test = TestResources.Create();
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333, unixNowMs);
        await test.CurrentlyShown.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs).Sales;
        await test.History.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShown(34, 5, unixNowMs);
        await test.CurrentlyShown.Update(document2, new CurrentlyShownQuery { WorldId = 34, ItemId = 5 });
        
        var sales2 = SeedDataGenerator.MakeHistory(34, 5, unixNowMs).Sales;
        await test.History.InsertSales(sales2, new HistoryQuery { WorldId = 34, ItemId = 5 });

        var result = await test.Controller.Get("5,5333", worldOrDc, entriesToReturn: int.MaxValue.ToString());
        var currentlyShown = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5, currentlyShown.ItemIds);
        Assert.Contains(5333, currentlyShown.ItemIds);
        Assert.Empty(currentlyShown.UnresolvedItemIds);
        Assert.Equal(2, currentlyShown.Items.Count);
        Assert.Null(currentlyShown.WorldId);
        Assert.Null(currentlyShown.WorldName);
        Assert.Equal("Crystal", currentlyShown.DcName);

        AssertCurrentlyShownDataCenter(
            document1,
            sales1,
            currentlyShown.Items.First(item => item.ItemId == document1.ItemId),
            unixNowMs,
            worldOrDc);
        AssertCurrentlyShownDataCenter(
            document2,
            sales2,
            currentlyShown.Items.First(item => item.ItemId == document2.ItemId),
            unixNowMs,
            worldOrDc);
    }

    [Theory]
    [InlineData("74")]
    [InlineData("Coeurl")]
    [InlineData("coEUrl")]
    public async Task Controller_Get_Succeeds_SingleItem_World_WhenNone(string worldOrDc)
    {
        var test = TestResources.Create();

        const int itemId = 5333;
        var result = await test.Controller.Get(itemId.ToString(), worldOrDc);

        var history = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(itemId, history.ItemId);
        Assert.Equal(74, history.WorldId);
        Assert.Equal("Coeurl", history.WorldName);
        Assert.Null(history.DcName);
        Assert.NotNull(history.Listings);
        Assert.Empty(history.Listings);
        Assert.NotNull(history.RecentHistory);
        Assert.Empty(history.RecentHistory);
        Assert.Equal(0, history.LastUploadTimeUnixMilliseconds);
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
        var test = TestResources.Create();

        var result = await test.Controller.Get("5333,5", worldOrDc);

        var history = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5, history.UnresolvedItemIds);
        Assert.Contains(5333, history.UnresolvedItemIds);
        Assert.Contains(5, history.ItemIds);
        Assert.Contains(5333, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal(74, history.WorldId);
        Assert.Equal(test.GameData.AvailableWorlds()[74], history.WorldName);
        Assert.Null(history.DcName);
    }

    [Theory]
    [InlineData("crystaL")]
    [InlineData("Crystal")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNone(string worldOrDc)
    {
        var test = TestResources.Create();

        const int itemId = 5333;
        var result = await test.Controller.Get(itemId.ToString(), worldOrDc);

        var history = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(itemId, history.ItemId);
        Assert.Equal("Crystal", history.DcName);
        Assert.NotNull(history.Listings);
        Assert.Empty(history.Listings);
        Assert.NotNull(history.RecentHistory);
        Assert.Empty(history.RecentHistory);
        Assert.Equal(0, history.LastUploadTimeUnixMilliseconds);
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
        var test = TestResources.Create();

        var result = await test.Controller.Get("5333,5", worldOrDc);

        var data = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5, data.UnresolvedItemIds);
        Assert.Contains(5333, data.UnresolvedItemIds);
        Assert.Contains(5, data.ItemIds);
        Assert.Contains(5333, data.ItemIds);
        Assert.Empty(data.Items);
        Assert.Equal("Crystal", data.DcName);
        Assert.Null(data.WorldId);
        Assert.Null(data.WorldName);
        Assert.All(data.Items, item => Assert.NotNull(item.Listings));
        Assert.All(data.Items, item => Assert.NotNull(item.RecentHistory));
    }

    [Fact]
    public async Task Controller_Get_Succeeds_SingleItem_World_WhenNotMarketable()
    {
        var test = TestResources.Create();

        const int itemId = 0;
        var result = await test.Controller.Get(itemId.ToString(), "74");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_World_WhenNotMarketable()
    {
        var test = TestResources.Create();

        var result = await test.Controller.Get("0, 494967295", "74");

        var history = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0, history.UnresolvedItemIds);
        Assert.Contains(494967295, history.UnresolvedItemIds);
        Assert.Empty(history.Items);
        Assert.Equal(74, history.WorldId);
        Assert.Null(history.DcName);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNotMarketable()
    {
        var test = TestResources.Create();

        const int itemId = 0;
        var result = await test.Controller.Get(itemId.ToString(), "Crystal");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNotMarketable()
    {
        var test = TestResources.Create();

        var result = await test.Controller.Get("0 ,494967295", "crystal");

        var history = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0, history.UnresolvedItemIds);
        Assert.Contains(494967295, history.UnresolvedItemIds);
        Assert.Contains(0, history.ItemIds);
        Assert.Contains(494967295, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal("Crystal", history.DcName);
        Assert.Null(history.WorldId);
    }
    
    [Fact]
    public async Task Controller_Get_Succeeds_SingleItem_Fields()
    {
        var test = TestResources.Create();
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333, unixNowMs);
        await test.CurrentlyShown.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs).Sales;
        await test.History.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var result = await test.Controller.Get("5333", "Crystal", fields: "listings.pricePerUnit,minPrice,recentHistory");
        var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

        var json = JsonSerializer.Serialize(currentlyShown, new JsonSerializerOptions { Converters = { new PartiallySerializableJsonConverterFactory() } });

        Assert.Matches(@"{""listings"":\[({""pricePerUnit"":\d+},?){100}],""recentHistory"":\[({""hq"":(false|true),""pricePerUnit"":\d+,""quantity"":\d+,""timestamp"":\d+,""worldName"":""Coeurl"",""worldID"":74,""buyerName"":null,""total"":\d+},?){5}],""minPrice"":\d+}", json);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_Fields()
    {
        var test = TestResources.Create();
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333, unixNowMs);
        await test.CurrentlyShown.Update(document1, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        
        var sales1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs).Sales;
        await test.History.InsertSales(sales1, new HistoryQuery { WorldId = 74, ItemId = 5333 });

        var document2 = SeedDataGenerator.MakeCurrentlyShown(34, 5, unixNowMs);
        await test.CurrentlyShown.Update(document2, new CurrentlyShownQuery { WorldId = 34, ItemId = 5 });
        
        var sales2 = SeedDataGenerator.MakeHistory(34, 5, unixNowMs).Sales;
        await test.History.InsertSales(sales2, new HistoryQuery { WorldId = 34, ItemId = 5 });

        var result = await test.Controller.Get("5,5333", "Crystal", fields: "items.listings.pricePerUnit,dcName,items.minPrice");
        var currentlyShown = (CurrentlyShownMultiView)Assert.IsType<OkObjectResult>(result).Value;

        var json = JsonSerializer.Serialize(currentlyShown, new JsonSerializerOptions { Converters = { new PartiallySerializableJsonConverterFactory() } });

        Assert.Matches(@"{""items"":\[{""listings"":\[({""pricePerUnit"":\d+},?){100}],""minPrice"":\d+},{""listings"":\[({""pricePerUnit"":\d+},?){100}],""minPrice"":\d+}],""dcName"":""Crystal""}", json);
    }

    private static void AssertCurrentlyShownValidWorld(CurrentlyShown document, List<Sale> sales, CurrentlyShownView currentlyShown, IGameDataProvider gameData)
    {
        Assert.Equal(document.ItemId, currentlyShown.ItemId);
        Assert.Equal(document.WorldId, currentlyShown.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[document.WorldId], currentlyShown.WorldName);
        Assert.Equal(document.LastUploadTimeUnixMilliseconds / 1000, currentlyShown.LastUploadTimeUnixMilliseconds / 1000);
        Assert.Null(currentlyShown.DcName);

        Assert.NotNull(currentlyShown.Listings);
        Assert.NotNull(currentlyShown.RecentHistory);

        currentlyShown.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);
        document.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        sales = sales.OrderByDescending(s => s.SaleTime).Take(currentlyShown.RecentHistory.Count).ToList();

        var listings = document.Listings.Select(l =>
        {
            l.PricePerUnit = (int)Math.Ceiling(l.PricePerUnit * 1.05);
            return l;
        }).ToList();

        Assert.All(currentlyShown.Listings.Select(l => (object)l.WorldId), Assert.Null);
        Assert.All(currentlyShown.Listings.Select(l => l.WorldName), Assert.Null);

        Assert.All(currentlyShown.RecentHistory.Select(s => (object)s.WorldId), Assert.Null);
        Assert.All(currentlyShown.RecentHistory.Select(s => s.WorldName), Assert.Null);

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

    private static void AssertCurrentlyShownDataCenter(CurrentlyShown anyWorldDocument, List<Sale> sales, CurrentlyShownView currentlyShown, long lastUploadTime, string worldOrDc)
    {
        Assert.Equal(anyWorldDocument.ItemId, currentlyShown.ItemId);
        Assert.Equal(lastUploadTime / 1000, currentlyShown.LastUploadTimeUnixMilliseconds / 1000);
        Assert.Equal(char.ToUpperInvariant(worldOrDc[0]) + worldOrDc[1..].ToLowerInvariant(), currentlyShown.DcName);
        Assert.Null(currentlyShown.WorldId);
        Assert.Null(currentlyShown.WorldName);

        Assert.NotNull(currentlyShown.Listings);
        Assert.NotNull(currentlyShown.RecentHistory);

        currentlyShown.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);
        anyWorldDocument.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
        sales = sales.OrderByDescending(s => s.SaleTime).Take(currentlyShown.RecentHistory.Count).ToList();

        var listings = anyWorldDocument.Listings.Select(l =>
        {
            l.PricePerUnit = (int)Math.Ceiling(l.PricePerUnit * 1.05);
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
}