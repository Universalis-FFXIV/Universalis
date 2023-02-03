using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

[Collection("Database collection")]
public class ListingStoreTests
{
    private readonly DbFixture _fixture;

    public ListingStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_Works()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, 2);
        await store.ReplaceLive(currentlyShown.Listings);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLiveRetrieveLive_Works()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, 3);
        await store.ReplaceLive(currentlyShown.Listings);
        var results = await store.RetrieveLive(new ListingQuery { ItemId = 3, WorldId = 93 });

        Assert.NotNull(results);
        Assert.All(currentlyShown.Listings.OrderBy(l => l.PricePerUnit).Zip(results), pair =>
        {
            var (expected, actual) = pair;
            Assert.Equal(expected.ListingId, actual.ListingId);
            Assert.Equal(3, actual.ItemId);
            Assert.Equal(93, actual.WorldId);
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
            Assert.Equal(new DateTimeOffset(expected.LastReviewTime).ToUnixTimeSeconds(),
                new DateTimeOffset(actual.LastReviewTime).ToUnixTimeSeconds());
            Assert.Equal(DateTimeKind.Utc, actual.LastReviewTime.Kind);
            Assert.Equal(expected.SellerId, actual.SellerId);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task DeleteLiveRetrieveLive_Works()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, 98);

        await store.ReplaceLive(currentlyShown.Listings);
        var query = new ListingQuery { ItemId = 98, WorldId = 93 };
        await store.DeleteLive(query);
        var results = await store.RetrieveLive(query);

        Assert.NotNull(results);
        Assert.Empty(results);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLiveRetrieveLiveMultiple_Works()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        for (var i = 0; i < 10; i++)
        {
            var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, 5);
            await store.ReplaceLive(currentlyShown.Listings);
            var results = await store.RetrieveLive(new ListingQuery { ItemId = 5, WorldId = 93 });

            Assert.NotNull(results);
            Assert.All(currentlyShown.Listings.OrderBy(l => l.PricePerUnit).Zip(results), pair =>
            {
                var (expected, actual) = pair;
                Assert.Equal(expected.ListingId, actual.ListingId);
                Assert.Equal(5, actual.ItemId);
                Assert.Equal(93, actual.WorldId);
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
                Assert.Equal(new DateTimeOffset(expected.LastReviewTime).ToUnixTimeSeconds(),
                    new DateTimeOffset(actual.LastReviewTime).ToUnixTimeSeconds());
                Assert.Equal(DateTimeKind.Utc, actual.LastReviewTime.Kind);
                Assert.Equal(expected.SellerId, actual.SellerId);
                Assert.Equal(DateTimeKind.Utc, actual.UpdatedAt.Kind);
                Assert.Equal(expected.Source, actual.Source);
            });
        }
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLiveRetrieveManyLive_Works()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var expectedListings = new Dictionary<int, IList<Listing>>();
        for (var i = 100; i < 105; i++)
        {
            var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, i);
            await store.ReplaceLive(currentlyShown.Listings);
            expectedListings[i] = currentlyShown.Listings;
        }

        var results = await store.RetrieveManyLive(new ListingManyQuery
            { ItemIds = Enumerable.Range(100, 105), WorldIds = new[] { 93 } });

        Assert.NotNull(results);
        for (var i = 100; i < 105; i++)
        {
            Assert.All(expectedListings[i].OrderBy(l => l.PricePerUnit).Zip(results[new WorldItemPair(93, i)]), pair =>
            {
                var (expected, actual) = pair;
                Assert.Equal(expected.ListingId, actual.ListingId);
                Assert.Equal(expected.ItemId, actual.ItemId);
                Assert.Equal(expected.WorldId, actual.WorldId);
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
                Assert.Equal(new DateTimeOffset(expected.LastReviewTime).ToUnixTimeSeconds(),
                    new DateTimeOffset(actual.LastReviewTime).ToUnixTimeSeconds());
                Assert.Equal(DateTimeKind.Utc, actual.LastReviewTime.Kind);
                Assert.Equal(expected.SellerId, actual.SellerId);
                Assert.Equal(DateTimeKind.Utc, actual.UpdatedAt.Kind);
                Assert.Equal(expected.Source, actual.Source);
            });
        }
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveLive_ReturnsEmpty_WhenMissing()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var results = await store.RetrieveLive(new ListingQuery { ItemId = 4, WorldId = 93 });
        Assert.NotNull(results);
        Assert.Empty(results);
    }

#if DEBUG
    [Fact]
#endif
    public async Task RetrieveManyLive_ReturnsEmpty_WhenMissing()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var results = await store.RetrieveManyLive(new ListingManyQuery
            { ItemIds = Enumerable.Range(200, 210), WorldIds = new[] { 93 } });
        Assert.NotNull(results);
        Assert.All(results, kvp =>
        {
            var (_, value) = kvp;
            Assert.NotNull(value);
            Assert.Empty(value);
        });
    }
}