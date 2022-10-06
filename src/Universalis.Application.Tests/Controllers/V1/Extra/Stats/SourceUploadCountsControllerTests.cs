using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1.Extra.Stats;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.AccessControl;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1.Extra.Stats;

public class SourceUploadCountsControllerTests
{
    [Fact]
    public async Task Controller_Get_Succeeds()
    {
        var dbAccess = new MockTrustedSourceDbAccess();
        var controller = new SourceUploadCountsController(dbAccess);
        
        const string key = "blah";
        using var sha512 = SHA512.Create();
        var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
        var document = new ApiKey(hash, "something", true);

        await dbAccess.Create(document);
        await dbAccess.Increment(new TrustedSourceQuery { ApiKeySha512 = document.TokenSha512});

        var result = await controller.Get();
        var counts = Assert.IsAssignableFrom<IEnumerable<SourceUploadCountView>>(result).ToList();

        Assert.Single(counts);
        Assert.Equal(document.Name, counts.First().Name);
        Assert.Equal(1U, counts.First().UploadCount);
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