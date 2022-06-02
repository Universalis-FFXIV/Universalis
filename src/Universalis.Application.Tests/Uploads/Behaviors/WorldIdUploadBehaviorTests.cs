using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class WorldIdUploadBehaviorTests
{
    [Fact]
    public void Behavior_DoesNotRun_WithoutWorldId()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockWorldUploadCountDbAccess();
        var behavior = new WorldIdUploadBehavior(gameData, dbAccess);

        var upload = new UploadParameters();

        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public async Task Behavior_Succeeds()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockWorldUploadCountDbAccess();
        var behavior = new WorldIdUploadBehavior(gameData, dbAccess);

        var upload = new UploadParameters
        {
            WorldId = 74,
        };

        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(null, upload);
        Assert.Null(result);

        // var data = (await dbAccess.GetWorldUploadCounts()).ToList();
        // Assert.NotNull(data);
        // Assert.Single(data);
        // Assert.Equal(gameData.AvailableWorlds()[upload.WorldId.Value], data[0].WorldName);
    }

    [Fact]
    public async Task Behavior_Fails_WhenWorldIdInvalid()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockWorldUploadCountDbAccess();
        var behavior = new WorldIdUploadBehavior(gameData, dbAccess);

        var upload = new UploadParameters
        {
            WorldId = 0,
        };

        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(null, upload);
        Assert.IsType<NotFoundObjectResult>(result);

        var data = (await dbAccess.GetWorldUploadCounts()).ToList();
        Assert.Empty(data);
    }
}