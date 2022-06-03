using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Views.V1.Extra.Stats;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats;

public class RecentlyUpdatedItemsControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var dbAccess = new MockRecentlyUpdatedItemsDbAccess();
        var controller = new RecentlyUpdatedItemsController(dbAccess);

        await dbAccess.Push(5333);

        var result = await controller.Get();
        Assert.IsAssignableFrom<RecentlyUpdatedItemsView>(result);

        // Assert.Single(counts);
        // Assert.Equal(5333U, counts[0]);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_WhenNone()
    {
        var dbAccess = new MockRecentlyUpdatedItemsDbAccess();
        var controller = new RecentlyUpdatedItemsController(dbAccess);

        var result = await controller.Get();
        Assert.IsAssignableFrom<RecentlyUpdatedItemsView>(result);

        // Assert.Empty(counts);
    }
}