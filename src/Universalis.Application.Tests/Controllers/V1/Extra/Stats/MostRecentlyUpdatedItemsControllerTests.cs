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

public class MostRecentlyUpdatedItemsControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new MostRecentlyUpdatedItemsController(gameData, dbAccess);

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
        var lru = (MostRecentlyUpdatedItemsView)Assert.IsType<OkObjectResult>(result).Value;

        Assert.All(lru.Items.Select(o => o.WorldName), Assert.NotNull);

        var lastTimestamp = lru.Items[0].LastUploadTimeUnixMilliseconds;
        for (var i = 1; i < lru.Items.Count; i++)
        {
            var item = lru.Items[i];
            Assert.True(lastTimestamp >= item.LastUploadTimeUnixMilliseconds, $"Failed on iteration {i}/{lru.Items.Count}");
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
        var controller = new MostRecentlyUpdatedItemsController(gameData, dbAccess);

        var result = await controller.Get(world, dc, "");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_WhenNone()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockMostRecentlyUpdatedDbAccess();
        var controller = new MostRecentlyUpdatedItemsController(gameData, dbAccess);

        var result = await controller.Get("", "Crystal", "");
        Assert.IsType<OkObjectResult>(result);
    }
}