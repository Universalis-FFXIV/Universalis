using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats;

public class RecentlyUpdatedItemsControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var dbAccess = new MockRecentlyUpdatedItemsDbAccess();
        var gameData = new MockGameDataProvider();
        var controller = new RecentlyUpdatedItemsController(dbAccess, gameData);

        await dbAccess.Push(5333);

        var result = await controller.Get();
        Assert.IsAssignableFrom<RecentlyUpdatedItemsView>(result);

        Assert.Single(result.Items);
        Assert.Equal(5333U, result.Items[0]);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_WhenNone()
    {
        var dbAccess = new MockRecentlyUpdatedItemsDbAccess();
        var gameData = new MockGameDataProvider();
        var controller = new RecentlyUpdatedItemsController(dbAccess, gameData);

        var result = await controller.Get();
        Assert.IsAssignableFrom<RecentlyUpdatedItemsView>(result);

        Assert.Empty(result.Items);
    }
}