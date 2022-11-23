using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1.Extra.Stats;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats;

public class TradeVolumeControllerTests
{
    [Fact]
    public async Task Controller_Get_Suceeds()
    {
        var gameData = new MockGameDataProvider();
        var saleStatistics = new MockSaleStatisticsDbAccess();
        var controller = new TradeVolumeController(saleStatistics, gameData);
        var startTime = new DateTimeOffset(2020, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2020, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var result = await controller.Get("Brynhildr", null, 5333,
            startTime.ToUnixTimeMilliseconds(), endTime.ToUnixTimeMilliseconds());
        var data = (TradeVolumeView)Assert.IsType<OkObjectResult>(result).Value;
        Assert.Equal(100, data.Units);
        Assert.Equal(100, data.Gil);
        Assert.Equal(startTime.ToUnixTimeMilliseconds(), data.From);
        Assert.Equal(endTime.ToUnixTimeMilliseconds(), data.To);
    }

    [Fact]
    public async Task Controller_Get_Fails_WithInvalidWorld()
    {
        var gameData = new MockGameDataProvider();
        var saleStatistics = new MockSaleStatisticsDbAccess();
        var controller = new TradeVolumeController(saleStatistics, gameData);
        var startTime = new DateTimeOffset(2020, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2020, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var result = await controller.Get("0", null, 5333,
            startTime.ToUnixTimeMilliseconds(), endTime.ToUnixTimeMilliseconds());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Fails_WithInvalidDataCenter()
    {
        var gameData = new MockGameDataProvider();
        var saleStatistics = new MockSaleStatisticsDbAccess();
        var controller = new TradeVolumeController(saleStatistics, gameData);
        var startTime = new DateTimeOffset(2020, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2020, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var result = await controller.Get(null, "Hello", 5333,
            startTime.ToUnixTimeMilliseconds(), endTime.ToUnixTimeMilliseconds());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Fails_WithInvalidItem()
    {
        var gameData = new MockGameDataProvider();
        var saleStatistics = new MockSaleStatisticsDbAccess();
        var controller = new TradeVolumeController(saleStatistics, gameData);
        var startTime = new DateTimeOffset(2020, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2020, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var result = await controller.Get("Brynhildr", null, 0,
            startTime.ToUnixTimeMilliseconds(), endTime.ToUnixTimeMilliseconds());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Fails_WithNoWorldOrDataCenter()
    {
        var gameData = new MockGameDataProvider();
        var saleStatistics = new MockSaleStatisticsDbAccess();
        var controller = new TradeVolumeController(saleStatistics, gameData);
        var startTime = new DateTimeOffset(2020, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2020, 9, 2, 0, 0, 0, TimeSpan.Zero);
        var result = await controller.Get(null, null, 5333,
            startTime.ToUnixTimeMilliseconds(), endTime.ToUnixTimeMilliseconds());
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
