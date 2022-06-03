using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class ItemIdUploadBehaviorTests
{
    [Fact]
    public void Behavior_DoesNotRun_WithoutItemId()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockRecentlyUpdatedItemsDbAccess();
        var behavior = new ItemIdUploadBehavior(gameData);

        var upload = new UploadParameters();

        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public async Task Behavior_Succeeds()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockRecentlyUpdatedItemsDbAccess();
        var behavior = new ItemIdUploadBehavior(gameData);

        var upload = new UploadParameters
        {
            ItemId = 5333,
        };

        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(null, upload);
        Assert.Null(result);

        // var data = await dbAccess.Retrieve(new RecentlyUpdatedItemsQuery());
        // Assert.NotNull(data);
        // Assert.Single(data.Items);
        // Assert.Equal(upload.ItemId.Value, data.Items[0]);
    }

    [Fact]
    public async Task Behavior_Fails_WhenNotMarketable()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockRecentlyUpdatedItemsDbAccess();
        var behavior = new ItemIdUploadBehavior(gameData);

        var upload = new UploadParameters
        {
            ItemId = 0,
        };

        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(null, upload);
        Assert.IsType<NotFoundObjectResult>(result);

        var data = await dbAccess.Retrieve(new RecentlyUpdatedItemsQuery());
        Assert.Null(data);
    }
}