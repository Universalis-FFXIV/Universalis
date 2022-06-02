using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Views.V1.Extra.Stats;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats;

public class UploadCountHistoryControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var dbAccess = new MockUploadCountHistoryDbAccess();
        var controller = new UploadCountHistoryController(dbAccess);
            
        await dbAccess.Update(DateTimeOffset.Now.ToUnixTimeMilliseconds(), new List<double> { 1 });

        var result = await controller.Get();
        Assert.IsType<UploadCountHistoryView>(result);

        // Assert.Equal(1, counts.UploadCountByDay.Count);
        // Assert.Equal(1U, counts.UploadCountByDay[0]);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_WhenNone()
    {
        var dbAccess = new MockUploadCountHistoryDbAccess();
        var controller = new UploadCountHistoryController(dbAccess);

        var result = await controller.Get();
        Assert.IsType<UploadCountHistoryView>(result);

        // Assert.Equal(0, counts.UploadCountByDay.Count);
    }
}