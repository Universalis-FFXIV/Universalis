using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

[Collection("Database collection")]
public class DailyUploadCountStoreTests
{
    private readonly DbFixture _fixture;

    public DailyUploadCountStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }
    
#if DEBUG
    [Fact]
#endif
    public async Task Increment_Works()
    {
        var store = _fixture.Services.GetRequiredService<IDailyUploadCountStore>();
        await store.Increment();
    }
    
#if DEBUG
    [Fact]
#endif
    public async Task Increment_Updates_Counter()
    {
        var store = _fixture.Services.GetRequiredService<IDailyUploadCountStore>();
        await store.Increment();
        var counts = await store.GetUploadCounts();
        
        // These tests share state so asserts are complicated...
        Assert.NotEmpty(counts);
        Assert.True(counts[0] > 0);
    }
}