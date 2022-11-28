using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;
using Universalis.DbAccess.AccessControl;
using Universalis.Entities.AccessControl;

namespace Universalis.DbAccess.Tests.AccessControl;

public class ApiKeyStoreTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public ApiKeyStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Works()
    {
        var store = _fixture.Services.GetRequiredService<IApiKeyStore>();
        var apiKey = new ApiKey("hello", "world", false);

        await store.Insert(apiKey);
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieve_Works()
    {
        var store = _fixture.Services.GetRequiredService<IApiKeyStore>();
        var apiKey = new ApiKey("goodbye", "world", false);

        await store.Insert(apiKey);

        var result = await store.Retrieve("goodbye");

        Assert.Equal(apiKey.TokenSha512, result.TokenSha512);
        Assert.Equal(apiKey.Name, result.Name);
        Assert.Equal(apiKey.CanUpload, result.CanUpload);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Retrieve_Missing_ReturnsNull()
    {
        var store = _fixture.Services.GetRequiredService<IApiKeyStore>();
        var result = await store.Retrieve("why");

        Assert.Null(result);
    }
}
