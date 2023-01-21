using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Tests.Uploads;

[Collection("Database collection")]
public class FlaggedUploaderStoreTests
{
    private readonly DbFixture _fixture;

    public FlaggedUploaderStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Works()
    {
        var store = _fixture.Services.GetRequiredService<IFlaggedUploaderStore>();
        var flaggedUploader = new FlaggedUploader("15084143697577");

        await store.Insert(flaggedUploader);
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieve_Works()
    {
        var store = _fixture.Services.GetRequiredService<IFlaggedUploaderStore>();
        var flaggedUploader = new FlaggedUploader("15084143697578");

        await store.Insert(flaggedUploader);
        var result = await store.Retrieve("15084143697578");

        Assert.Equal(flaggedUploader.IdSha256, result.IdSha256);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Retrieve_Missing_ReturnsNull()
    {
        var store = _fixture.Services.GetRequiredService<IFlaggedUploaderStore>();
        var result = await store.Retrieve("15084143697579");

        Assert.Null(result);
    }
}
