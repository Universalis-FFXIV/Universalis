using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class DailyUploadIncrementUploadBehaviorTests
{
    [Fact]
    public async Task Behavior_Succeeds()
    {
        var dbAccess = new MockUploadCountHistoryDbAccess();
        var behavior = new DailyUploadIncrementUploadBehavior(dbAccess);

        var upload = new UploadParameters();
        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(null, upload);
        Assert.Null(result);

        // var data = await dbAccess.Retrieve(new UploadCountHistoryQuery());

        // Assert.NotNull(data);
        // Assert.Single(data.UploadCountByDay);
        // Assert.False(data.LastPush == 0);
        // Assert.Equal(1U, data.UploadCountByDay[0]);
    }
}