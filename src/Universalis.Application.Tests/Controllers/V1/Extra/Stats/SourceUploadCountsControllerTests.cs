using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats;

public class SourceUploadCountsControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var dbAccess = new MockTrustedSourceDbAccess();
        var controller = new SourceUploadCountsController(dbAccess);

        var document = new TrustedSource
        {
            ApiKeySha512 = "2A",
            Name = "test runner",
            UploadCount = 0,
        };
        await dbAccess.Create(document);

        var query = new TrustedSourceQuery { ApiKeySha512 = document.ApiKeySha512 };
        await dbAccess.Increment(document.Name);

        var result = await controller.Get();
        var counts = Assert.IsAssignableFrom<IEnumerable<SourceUploadCountView>>(result).ToList();

        Assert.Single(counts);
        Assert.Equal(document.Name, counts.First().Name);
        Assert.Equal(1U, document.UploadCount + 1);
    }

    [Fact]
    public async Task Controller_Get_Succeeds_WhenNone()
    {
        var dbAccess = new MockTrustedSourceDbAccess();
        var controller = new SourceUploadCountsController(dbAccess);

        var result = await controller.Get();
        var counts = Assert.IsAssignableFrom<IEnumerable<SourceUploadCountView>>(result).ToList();

        Assert.Empty(counts);
    }
}