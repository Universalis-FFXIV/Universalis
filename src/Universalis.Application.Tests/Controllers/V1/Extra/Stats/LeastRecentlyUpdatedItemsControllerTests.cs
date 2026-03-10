using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats;

public class LeastRecentlyUpdatedItemsControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        foreach (var itemId in Enumerable.Range(1, 12000))
        {
            await dbAccess.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
        }

        var result = await controller.Get("coeurl", "", "");
        var lru = (LeastRecentlyUpdatedItemsView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(lru);
        Assert.All(lru.Items.Select(o => o.WorldName), Assert.NotNull);

        var lastTimestamp = lru.Items[0].LastUploadTimeUnixMilliseconds;
        for (var i = 1; i < lru.Items.Count; i++)
        {
            var item = lru.Items[i];
            Assert.True(lastTimestamp <= item.LastUploadTimeUnixMilliseconds, $"Failed on iteration {i}/{lru.Items.Count}");
            lastTimestamp = item.LastUploadTimeUnixMilliseconds;
        }
    }

    [Theory]
    [InlineData("k", "")]
    [InlineData("", "k")]
    [InlineData("", "")]
    public async Task Controller_Get_Fails_WhenServerInvalid(string world, string dc)
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        var result = await controller.Get(world, dc, "");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_WhenNone()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        var result = await controller.Get("", "Crystal", "");
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Get_DcQuery_WithData_ReturnsRequestedCount()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        // Push data for both worlds in the Crystal DC (74=Coeurl, 34=Brynhildr)
        for (var i = 1; i <= 50; i++)
        {
            await dbAccess.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = i,
                LastUploadTimeUnixMilliseconds = i * 1000.0,
            });
            await dbAccess.Push(34, new WorldItemUpload
            {
                WorldId = 34,
                ItemId = i + 1000,
                LastUploadTimeUnixMilliseconds = i * 1000.0 + 500,
            });
        }

        var result = await controller.Get("", "Crystal", "10");
        var lru = (LeastRecentlyUpdatedItemsView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(lru);
        Assert.Equal(10, lru.Items.Count);
        Assert.All(lru.Items, item => Assert.NotNull(item.WorldName));
    }

    [Fact]
    public async Task Controller_Get_RegionQuery_WithData_ReturnsRequestedCount()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        // Push data for both worlds in North-America region
        for (var i = 1; i <= 50; i++)
        {
            await dbAccess.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = i,
                LastUploadTimeUnixMilliseconds = i * 1000.0,
            });
            await dbAccess.Push(34, new WorldItemUpload
            {
                WorldId = 34,
                ItemId = i + 1000,
                LastUploadTimeUnixMilliseconds = i * 1000.0 + 500,
            });
        }

        // Query by region name via the "world" parameter (same as ?world=Europe from prior bug report about this)
        var result = await controller.Get("North-America", "", "20");
        var lru = (LeastRecentlyUpdatedItemsView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(lru);
        Assert.Equal(20, lru.Items.Count);
        Assert.All(lru.Items, item => Assert.NotNull(item.WorldName));
    }

    [Fact]
    public async Task Controller_Get_DcQuery_WithData_ItemsSortedByTimestampAscending()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        // Push data for both worlds with interleaved timestamps
        for (var i = 1; i <= 50; i++)
        {
            await dbAccess.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = i,
                LastUploadTimeUnixMilliseconds = i * 2000.0,
            });
            await dbAccess.Push(34, new WorldItemUpload
            {
                WorldId = 34,
                ItemId = i + 1000,
                LastUploadTimeUnixMilliseconds = i * 2000.0 - 1000,
            });
        }

        var result = await controller.Get("", "Crystal", "10");
        var lru = (LeastRecentlyUpdatedItemsView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(lru);
        Assert.Equal(10, lru.Items.Count);

        // Verify ascending timestamp order (least recently updated first)
        var lastTimestamp = lru.Items[0].LastUploadTimeUnixMilliseconds;
        for (var i = 1; i < lru.Items.Count; i++)
        {
            Assert.True(lastTimestamp <= lru.Items[i].LastUploadTimeUnixMilliseconds,
                $"Item at index {i} has timestamp {lru.Items[i].LastUploadTimeUnixMilliseconds} " +
                $"which is less than previous {lastTimestamp}");
            lastTimestamp = lru.Items[i].LastUploadTimeUnixMilliseconds;
        }
    }

    [Fact]
    public async Task Controller_Get_DcQuery_ContainsItemsFromMultipleWorlds()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        // Push data with interleaved timestamps so both worlds appear in results
        for (var i = 1; i <= 50; i++)
        {
            await dbAccess.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = i,
                LastUploadTimeUnixMilliseconds = i * 2000.0,
            });
            await dbAccess.Push(34, new WorldItemUpload
            {
                WorldId = 34,
                ItemId = i + 1000,
                LastUploadTimeUnixMilliseconds = i * 2000.0 - 1000,
            });
        }

        var result = await controller.Get("", "Crystal", "20");
        var lru = (LeastRecentlyUpdatedItemsView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.NotNull(lru);
        Assert.Equal(20, lru.Items.Count);

        // Both worlds should be represented in the results
        var worldIds = lru.Items.Select(item => item.WorldId).Distinct().ToList();
        Assert.Contains(74, worldIds);
        Assert.Contains(34, worldIds);
    }

    /// <summary>
    /// Non-marketable items (stale data from items removed from the market board in game patches)
    /// should never appear in the response, even when they have the oldest timestamps.
    /// </summary>
    [Fact]
    public async Task Controller_Get_DcQuery_WithNonMarketableItems_OnlyReturnsMarketableItems()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new LeastRecentlyUpdatedItemsController(gameData, dbAccess);

        // MockGameDataProvider.MarketableItemIds() returns range 1-35000
        foreach (var worldId in new[] { 74, 34 })
        {
            // Non-marketable items with very old timestamps
            for (var i = 0; i < 10; i++)
            {
                await dbAccess.Push(worldId, new WorldItemUpload
                {
                    WorldId = worldId,
                    ItemId = 40000 + i,
                    LastUploadTimeUnixMilliseconds = i * 1000.0 + worldId,
                });
            }

            // Marketable items with newer timestamps
            for (var i = 0; i < 50; i++)
            {
                await dbAccess.Push(worldId, new WorldItemUpload
                {
                    WorldId = worldId,
                    ItemId = i + 1,
                    LastUploadTimeUnixMilliseconds = 100000 + i * 1000.0 + worldId,
                });
            }
        }

        var result = await controller.Get("", "Crystal", "10");
        var lru = (LeastRecentlyUpdatedItemsView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.Equal(10, lru.Items.Count);
        Assert.All(lru.Items, item => Assert.True(
            gameData.MarketableItemIds().Contains(item.ItemId),
            $"Item {item.ItemId} should be marketable"));
    }
}