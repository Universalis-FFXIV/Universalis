﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Tests;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

public class HistoryControllerTests
{
    private class TestResources
    {
        public IGameDataProvider GameData { get; private init; }
        public IHistoryDbAccess History { get; private init; }
        public HistoryController Controller { get; private init; }

        public static TestResources Create()
        {
            var gameData = new MockGameDataProvider();
            var historyDb = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, historyDb);
            return new TestResources
            {
                GameData = gameData,
                History = historyDb,
                Controller = controller,
            };
        }
    }

    [Theory]
    [InlineData("74", "")]
    [InlineData("Coeurl", " bingus4645")]
    [InlineData("coEUrl", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_World(string worldOrDc, string entriesToReturn)
    {
        var test = TestResources.Create();

        var document = SeedDataGenerator.MakeHistory(74, 5333);
        await test.History.Create(document);

        var result = await test.Controller.Get("5333", worldOrDc, entriesToReturn);
        var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

        var unixNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        AssertHistoryValidWorld(document, history, test.GameData, unixNowMs);
    }

    [Theory]
    [InlineData("74", "")]
    [InlineData("Coeurl", " bingus4645")]
    [InlineData("coEUrl", "50")]
    public async Task Controller_Get_Succeeds_MultiItem_World(string worldOrDc, string entriesToReturn)
    {
        var test = TestResources.Create();
        var unixNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs);
        await test.History.Create(document1);

        var document2 = SeedDataGenerator.MakeHistory(74, 5, unixNowMs);
        await test.History.Create(document2);

        var result = await test.Controller.Get("5,5333", worldOrDc, entriesToReturn);
        var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(history);
        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.UnresolvedItemIds);
        Assert.Equal(2, history.Items.Count);
        Assert.Equal(74U, history.WorldId);
        Assert.Equal(test.GameData.AvailableWorlds()[74], history.WorldName);
        Assert.Null(history.DcName);

        AssertHistoryValidWorld(document1, history.Items.First(item => item.ItemId == document1.ItemId), test.GameData,
            unixNowMs);
        AssertHistoryValidWorld(document2, history.Items.First(item => item.ItemId == document2.ItemId), test.GameData,
            unixNowMs);
    }

    [Theory]
    [InlineData("crystaL", "")]
    [InlineData("Crystal", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter(string worldOrDc, string entriesToReturn)
    {
        var test = TestResources.Create();

        var document1 = SeedDataGenerator.MakeHistory(74, 5333);
        await test.History.Create(document1);

        var document2 = SeedDataGenerator.MakeHistory(34, 5333);
        await test.History.Create(document2);

        var result = await test.Controller.Get("5333", worldOrDc, entriesToReturn);
        var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

        var sales = document1.Sales.Concat(document2.Sales).ToList();

        AssertHistoryValidDataCenter(
            document1,
            history,
            sales,
            worldOrDc);
    }

    [Theory]
    [InlineData("crystaL", "")]
    [InlineData("Crystal", "50")]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter(string worldOrDc, string entriesToReturn)
    {
        var test = TestResources.Create();
        var unixNowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var document1 = SeedDataGenerator.MakeHistory(74, 5333, unixNowMs);
        await test.History.Create(document1);

        var document2 = SeedDataGenerator.MakeHistory(34, 5, unixNowMs);
        await test.History.Create(document2);

        var result = await test.Controller.Get("5,5333", worldOrDc, entriesToReturn);
        var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.UnresolvedItemIds);
        Assert.Equal(2, history.Items.Count);
        Assert.Null(history.WorldId);
        Assert.Null(history.WorldName);
        Assert.Equal("Crystal", history.DcName);

        AssertHistoryValidDataCenter(
            document1,
            history.Items.First(item => item.ItemId == document1.ItemId),
            document1.Sales,
            worldOrDc);
        AssertHistoryValidDataCenter(
            document2,
            history.Items.First(item => item.ItemId == document2.ItemId),
            document2.Sales,
            worldOrDc);
    }

    [Theory]
    [InlineData("74", "")]
    [InlineData("Coeurl", " bingus4645")]
    [InlineData("coEUrl", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_World_WhenNone(string worldOrDc, string entriesToReturn)
    {
        var test = TestResources.Create();

        const uint itemId = 5333;
        var result = await test.Controller.Get(itemId.ToString(), worldOrDc, entriesToReturn);

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
        var test = TestResources.Create();

        var result = await test.Controller.Get("5333,5", worldOrDc, entriesToReturn);

        var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(5U, history.UnresolvedItemIds);
        Assert.Contains(5333U, history.UnresolvedItemIds);
        Assert.Contains(5U, history.ItemIds);
        Assert.Contains(5333U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal(74U, history.WorldId);
        Assert.Equal(test.GameData.AvailableWorlds()[74], history.WorldName);
        Assert.Null(history.DcName);
    }

    [Theory]
    [InlineData("crystaL", "")]
    [InlineData("Crystal", "50")]
    public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNone(string worldOrDc, string entriesToReturn)
    {
        var test = TestResources.Create();

        const uint itemId = 5333;
        var result = await test.Controller.Get(itemId.ToString(), worldOrDc, entriesToReturn);

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
        var test = TestResources.Create();

        var result = await test.Controller.Get("5333,5", worldOrDc, entriesToReturn);

        var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

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
        var test = TestResources.Create();

        const uint itemId = 0;
        var result = await test.Controller.Get(itemId.ToString(), "74", "");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_World_WhenNotMarketable()
    {
        var test = TestResources.Create();

        var result = await test.Controller.Get("0, 4294967295", "74", "");

        var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0U, history.UnresolvedItemIds);
        Assert.Contains(4294967295U, history.UnresolvedItemIds);
        Assert.Empty(history.Items);
        Assert.Equal(74U, history.WorldId);
        Assert.Null(history.DcName);
    }

    [Fact]
    public async Task Controller_Get_Fails_SingleItem_DataCenter_WhenNotMarketable()
    {
        var test = TestResources.Create();

        const uint itemId = 0;
        var result = await test.Controller.Get(itemId.ToString(), "Crystal", "");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNotMarketable()
    {
        var test = TestResources.Create();

        var result = await test.Controller.Get("0 ,4294967295", "crystal", "");

        var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Contains(0U, history.UnresolvedItemIds);
        Assert.Contains(4294967295U, history.UnresolvedItemIds);
        Assert.Contains(0U, history.ItemIds);
        Assert.Contains(4294967295U, history.ItemIds);
        Assert.Empty(history.Items);
        Assert.Equal("Crystal", history.DcName);
        Assert.Null(history.WorldId);
    }

    private static void AssertHistoryValidWorld(History document, HistoryView history, IGameDataProvider gameData,
        long unixNowMs)
    {
        document.Sales.Sort((a, b) => (int)(b.SaleTime - a.SaleTime).TotalMilliseconds);

        Assert.Equal(document.ItemId, history.ItemId);
        Assert.Equal(document.WorldId, history.WorldId);
        Assert.Equal(gameData.AvailableWorlds()[document.WorldId], history.WorldName);
        Assert.Null(history.DcName);
        Assert.NotNull(history.Sales);

        Assert.All(history.Sales.Select(s => (object)s.WorldId), Assert.Null);
        Assert.All(history.Sales.Select(s => s.WorldName), Assert.Null);

        Assert.True(history.SaleVelocity > 0);
        Assert.True(history.SaleVelocityNq > 0);
        Assert.True(history.SaleVelocityHq > 0);
    }

    private static void AssertHistoryValidDataCenter(History anyWorldDocument, HistoryView history, List<Sale> sales,
        string worldOrDc)
    {
        sales.Sort((a, b) => (int)(b.SaleTime - a.SaleTime).TotalMilliseconds);

        Assert.All(history.Sales.Select(s => (object)s.WorldId), Assert.NotNull);
        Assert.All(history.Sales.Select(s => s.WorldName), Assert.NotNull);

        Assert.Equal(anyWorldDocument.ItemId, history.ItemId);
        Assert.Equal(char.ToUpperInvariant(worldOrDc[0]) + worldOrDc[1..].ToLowerInvariant(), history.DcName);
        Assert.Null(history.WorldId);
        Assert.Null(history.WorldName);
        Assert.NotNull(history.Sales);

        Assert.True(history.SaleVelocity > 0);
        Assert.True(history.SaleVelocityNq > 0);
        Assert.True(history.SaleVelocityHq > 0);
    }
}