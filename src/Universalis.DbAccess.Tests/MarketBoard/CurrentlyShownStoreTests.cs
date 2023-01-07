using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Xunit;
using System.Linq;

namespace Universalis.DbAccess.Tests.MarketBoard;

public class CurrentlyShownStoreTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public CurrentlyShownStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task SetData_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(74, 2);
        await store.SetData(currentlyShown);
    }

#if DEBUG
    [Fact]
#endif
    public async Task SetDataGetData_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(74, 3);
        await store.SetData(currentlyShown);
        var results = await store.GetData(74, 3);

        Assert.NotNull(results);
        Assert.Equal(currentlyShown.WorldId, results.WorldId);
        Assert.Equal(currentlyShown.ItemId, results.ItemId);
        Assert.Equal(currentlyShown.LastUploadTimeUnixMilliseconds, results.LastUploadTimeUnixMilliseconds);
        Assert.Equal(currentlyShown.UploadSource, results.UploadSource);
        Assert.All(currentlyShown.Listings.Zip(results.Listings), pair =>
        {
            var (expected, actual) = pair;
            Assert.Equal(expected.ListingId, actual.ListingId);
            Assert.Equal(expected.Hq, actual.Hq);
            Assert.Equal(expected.OnMannequin, actual.OnMannequin);
            Assert.Equal(expected.PricePerUnit, actual.PricePerUnit);
            Assert.Equal(expected.Quantity, actual.Quantity);
            Assert.Equal(expected.RetainerName, actual.RetainerName);
            Assert.Equal(expected.RetainerId, actual.RetainerId);
            Assert.Equal(expected.RetainerCityId, actual.RetainerCityId);
            Assert.Equal(expected.DyeId, actual.DyeId);
            Assert.Equal(expected.CreatorId, actual.CreatorId);
            Assert.Equal(expected.CreatorName, actual.CreatorName);
            Assert.Equal(expected.LastReviewTime, actual.LastReviewTime);
            Assert.Equal(expected.SellerId, actual.SellerId);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task GetData_ReturnsEmpty_WhenMissing()
    {
        var store = _fixture.Services.GetRequiredService<ICurrentlyShownStore>();
        var results = await store.GetData(74, 4);
        Assert.NotNull(results);
        Assert.Equal(0, results.WorldId);
        Assert.Equal(0, results.ItemId);
        Assert.Equal(0, results.LastUploadTimeUnixMilliseconds);
        Assert.Equal("", results.UploadSource);
        Assert.Empty(results.Listings);
    }
}
