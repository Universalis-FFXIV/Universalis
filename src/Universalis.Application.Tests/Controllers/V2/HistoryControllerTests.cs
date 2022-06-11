using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V2;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1;
using Universalis.Application.Views.V2;
using Universalis.DataTransformations;
using Universalis.DbAccess.Tests;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V2;

public class HistoryControllerTests
{
    private const long WeekLength = 604800000L;

    [Theory]
    [InlineData("74", "")]
    [InlineData("Coeurl", " bingus4645")]
    [InlineData("coEUrl", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_World(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var document = SeedDataGenerator.MakeHistory(74, 5333, now);
        await dbAccess.Create(document);

        var result = await controller.Get("5333", worldOrDc, entriesToReturn);
        var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

        AssertHistoryValidWorld(document, history, gameData, entriesToReturn, now);
    }

    [Theory]
    [InlineData("74", "")]
    [InlineData("Coeurl", " bingus4645")]
    [InlineData("coEUrl", "50")]
    public async Task Controller_Get_Succeeds_MultiItem_World(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);
        var lastUploadTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeHistory(74, 5333, lastUploadTime);
        await dbAccess.Create(document1);

        var document2 = SeedDataGenerator.MakeHistory(74, 5, lastUploadTime);
        await dbAccess.Create(document2);

        var result = await controller.Get("5,5333", worldOrDc, entriesToReturn);
        var history = (HistoryMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.UnresolvedItemIds);
        Assert.Equal(2, history.Items.Count);
        Assert.Equal(74U, history.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[74], history.WorldName);
        Assert.Null(history.DcName);

        AssertHistoryValidWorld(document1, history.Items.First(item => item.Key == document1.ItemId).Value, gameData, entriesToReturn, lastUploadTime);
        AssertHistoryValidWorld(document2, history.Items.First(item => item.Key == document2.ItemId).Value, gameData, entriesToReturn, lastUploadTime);
    }

    [Theory]
    [InlineData("crystaL", "")]
    [InlineData("Crystal", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        var document1 = SeedDataGenerator.MakeHistory(74, 5333);
        await dbAccess.Create(document1);

        var document2 = SeedDataGenerator.MakeHistory(34, 5333);
        await dbAccess.Create(document2);

        var result = await controller.Get("5333", worldOrDc, entriesToReturn);
        var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

        var sales = document1.Sales.Concat(document2.Sales).ToList();
       
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        AssertHistoryValidDataCenter(
            document1,
            history,
            sales,
            now,
            worldOrDc,
            entriesToReturn,
            now);
    }

    [Theory]
    [InlineData("crystaL", "")]
    [InlineData("Crystal", "50")]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);
        var lastUploadTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var document1 = SeedDataGenerator.MakeHistory(74, 5333, lastUploadTime);
        await dbAccess.Create(document1);

        var document2 = SeedDataGenerator.MakeHistory(34, 5, lastUploadTime);
        await dbAccess.Create(document2);

        var result = await controller.Get("5,5333", worldOrDc, entriesToReturn);
        var history = (HistoryMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.UnresolvedItemIds);
        Assert.Equal(2, history.Items.Count);
        Assert.Null(history.WorldId);
        Assert.Null(history.WorldName);
        Assert.Equal("Crystal", history.DcName);

        AssertHistoryValidDataCenter(
            document1,
            history.Items.First(item => item.Key == document1.ItemId).Value,
            document1.Sales,
            lastUploadTime,
            worldOrDc,
            entriesToReturn,
            lastUploadTime);
        AssertHistoryValidDataCenter(
            document2,
            history.Items.First(item => item.Key == document2.ItemId).Value,
            document2.Sales,
            lastUploadTime,
            worldOrDc,
            entriesToReturn,
            lastUploadTime);
    }

    [Theory]
    [InlineData("74", "")]
    [InlineData("Coeurl", " bingus4645")]
    [InlineData("coEUrl", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_World_WhenNone(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        const uint itemId = 5333;
        var result = await controller.Get(itemId.ToString(), worldOrDc, entriesToReturn);

        var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(itemId, history.ItemId);
        Assert.Equal(74U, history.WorldId);
        Assert.Equal("Coeurl", history.WorldName);
        Assert.Null(history.DcName);
        Assert.NotNull(history.Sales);
        Assert.Empty(history.Sales);
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
    [InlineData("74", "")]
    [InlineData("Coeurl", " bingus4645")]
    [InlineData("coEUrl", "50")]
    public async Task Controller_Get_Succeeds_MultiItem_World_WhenNone(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        var result = await controller.Get("5333,5", worldOrDc, entriesToReturn);

        var history = (HistoryMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

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
    [InlineData("crystaL", "")]
    [InlineData("Crystal", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNone(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        const uint itemId = 5333;
        var result = await controller.Get(itemId.ToString(), worldOrDc, entriesToReturn);

        var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(itemId, history.ItemId);
        Assert.Equal("Crystal", history.DcName);
        Assert.NotNull(history.Sales);
        Assert.Empty(history.Sales);
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
    [InlineData("crystaL", "")]
    [InlineData("Crystal", "50")]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNone(string worldOrDc, string entriesToReturn)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        var result = await controller.Get("5333,5", worldOrDc, entriesToReturn);

        var history = (HistoryMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, history.UnresolvedItemIds);
        Assert.Contains(5333U, history.UnresolvedItemIds);
        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal("Crystal", history.DcName);
        Assert.Null(history.WorldId);
    }

    [Fact]
    public async Task Controller_Get_Fails_SingleItem_World_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        const uint itemId = 0;
        var result = await controller.Get(itemId.ToString(), "74", "");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_World_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        var result = await controller.Get("0, 4294967295", "74", "");

        var history = (HistoryMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0U, history.UnresolvedItemIds);
        Assert.Contains(4294967295U, history.UnresolvedItemIds);
        Assert.Empty(history.Items);
        Assert.Equal(74U, history.WorldId);
        Assert.Null(history.DcName);
    }

    [Fact]
    public async Task Controller_Get_Fails_SingleItem_DataCenter_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        const uint itemId = 0;
        var result = await controller.Get(itemId.ToString(), "Crystal", "");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockHistoryDbAccess();
        var controller = new HistoryController(gameData, dbAccess);

        var result = await controller.Get("0 ,4294967295", "crystal", "");

        var history = (HistoryMultiViewV2)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0U, history.UnresolvedItemIds);
        Assert.Contains(4294967295U, history.UnresolvedItemIds);
        Assert.Contains(0U, history.ItemIds);
        Assert.Contains(4294967295U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal("Crystal", history.DcName);
        Assert.Null(history.WorldId);
    }

    private static void AssertHistoryValidWorld(History document, HistoryView history, IGameDataProvider gameData, string entriesToReturn, long unixNowMs)
    {
        document.Sales.Sort((a, b) => (int)(b.SaleTime - a.SaleTime).TotalMilliseconds);

        var nqSales = document.Sales.Where(s => !s.Hq).ToList();
        var hqSales = document.Sales.Where(s => s.Hq).ToList();

        Assert.Equal(document.ItemId, history.ItemId);
        Assert.Equal(document.WorldId, history.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[document.WorldId], history.WorldName);
        Assert.Null(history.DcName);
        Assert.NotNull(history.Sales);

        Assert.All(history.Sales.Select(s => (object)s.WorldId), Assert.Null);
        Assert.All(history.Sales.Select(s => s.WorldName), Assert.Null);

        Assert.True(IsSorted(history.StackSizeHistogram));
        Assert.True(IsSorted(history.StackSizeHistogramNq));
        Assert.True(IsSorted(history.StackSizeHistogramHq));

        Assert.Equal(Statistics.VelocityPerDay(document.Sales
                .Select(s => s.SaleTime.ToUnixTimeMilliseconds()), unixNowMs, WeekLength),
            history.SaleVelocity);
        Assert.Equal(Statistics.VelocityPerDay(nqSales
                .Select(s => s.SaleTime.ToUnixTimeMilliseconds()), unixNowMs, WeekLength),
            history.SaleVelocityNq);
        Assert.Equal(Statistics.VelocityPerDay(hqSales
                .Select(s => s.SaleTime.ToUnixTimeMilliseconds()), unixNowMs, WeekLength),
            history.SaleVelocityHq);
    }

    private static void AssertHistoryValidDataCenter(History anyWorldDocument, HistoryView history, List<Sale> sales, long lastUploadTime, string worldOrDc, string entriesToReturn, long unixNowMs)
    {
        sales.Sort((a, b) => (int)(b.SaleTime - a.SaleTime).TotalMilliseconds);

        Assert.All(history.Sales.Select(s => (object)s.WorldId), Assert.NotNull);
        Assert.All(history.Sales.Select(s => s.WorldName), Assert.NotNull);

        Assert.Equal(anyWorldDocument.ItemId, history.ItemId);
        Assert.Equal(char.ToUpperInvariant(worldOrDc[0]) + worldOrDc[1..].ToLowerInvariant(), history.DcName);
        Assert.Null(history.WorldId);
        Assert.Null(history.WorldName);
        Assert.NotNull(history.Sales);

        Assert.True(IsSorted(history.StackSizeHistogram));
        Assert.True(IsSorted(history.StackSizeHistogramNq));
        Assert.True(IsSorted(history.StackSizeHistogramHq));

        Assert.True(history.SaleVelocity > 0);
        Assert.True(history.SaleVelocityNq > 0);
        Assert.True(history.SaleVelocityHq > 0);
    }

    private static bool IsSorted(IDictionary<int, int> dict)
    {
        var lastK = int.MinValue;
        foreach (var (k, _) in dict)
        {
            if (k < lastK)
            {
                return false;
            }
        }

        return true;
    }

    private static double Round(double value)
    {
        return Math.Round(value, 2);
    }
}