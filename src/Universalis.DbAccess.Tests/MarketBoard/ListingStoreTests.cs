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
            AssertEqual(expected, actual);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLiveRetrieveLive_Cached_Works()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(92, 3);
        await store.ReplaceLive(currentlyShown.Listings);
        await store.RetrieveLive(new ListingQuery { ItemId = 3, WorldId = 92 }); // Populate the cache
        var results = await store.RetrieveLive(new ListingQuery { ItemId = 3, WorldId = 92 });

        Assert.NotNull(results);
        Assert.All(currentlyShown.Listings.OrderBy(l => l.PricePerUnit).Zip(results), pair =>
        {
            var (expected, actual) = pair;
            AssertEqual(expected, actual);
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
                AssertEqual(expected, actual);
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

        // Also store some more data that we don't want to retrieve to make sure we're not being too lenient
        for (var i = 106; i < 110; i++)
        {
            var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, i);
            await store.ReplaceLive(currentlyShown.Listings);
        }

        var results = await store.RetrieveManyLive(new ListingManyQuery
            { ItemIds = Enumerable.Range(100, 105), WorldIds = new[] { 93 } });

        Assert.NotNull(results);
        for (var i = 100; i < 105; i++)
        {
            Assert.All(expectedListings[i].OrderBy(l => l.PricePerUnit).Zip(results[new WorldItemPair(93, i)]), pair =>
            {
                var (expected, actual) = pair;
                AssertEqual(expected, actual);
            });
        }

        for (var i = 106; i < 110; i++)
        {
            Assert.False(expectedListings.ContainsKey(i));
        }
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLiveRetrieveManyLive_Cached_Works()
    {
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var expectedListings = new Dictionary<int, IList<Listing>>();
        for (var i = 100; i < 105; i++)
        {
            var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, i);
            await store.ReplaceLive(currentlyShown.Listings);
            expectedListings[i] = currentlyShown.Listings;
        }

        // Also store some more data that we don't want to retrieve to make sure we're not being too lenient
        for (var i = 106; i < 110; i++)
        {
            var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, i);
            await store.ReplaceLive(currentlyShown.Listings);
        }

        await store.RetrieveManyLive(new ListingManyQuery
            { ItemIds = Enumerable.Range(100, 105), WorldIds = new[] { 93 } }); // Populate the cache
        var results = await store.RetrieveManyLive(new ListingManyQuery
            { ItemIds = Enumerable.Range(100, 105), WorldIds = new[] { 93 } });

        Assert.NotNull(results);
        for (var i = 100; i < 105; i++)
        {
            Assert.All(expectedListings[i].OrderBy(l => l.PricePerUnit).Zip(results[new WorldItemPair(93, i)]), pair =>
            {
                var (expected, actual) = pair;
                AssertEqual(expected, actual);
            });
        }

        for (var i = 106; i < 110; i++)
        {
            Assert.False(expectedListings.ContainsKey(i));
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

#if DEBUG
    [Fact]
#endif
    public async Task GetMinListing_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, 2);
        await store.ReplaceLive(currentlyShown.Listings);
        var result = await store.GetMinListing(93, 2);

        Assert.True(100 <= result?.World?.Nq?.UnitPrice);
        Assert.True(100 <= result?.World?.Hq?.UnitPrice);
        Assert.NotEqual(result?.World?.Nq?.UnitPrice, result?.World?.Hq?.UnitPrice);
        Assert.Equal(result?.World?.Nq?.UnitPrice, result?.Dc?.Nq?.UnitPrice);
        Assert.Equal(result?.World?.Hq?.UnitPrice, result?.Dc?.Hq?.UnitPrice);
        Assert.Equal(result?.World?.Nq?.UnitPrice, result?.Region?.Nq?.UnitPrice);
        Assert.Equal(result?.World?.Hq?.UnitPrice, result?.Region?.Hq?.UnitPrice);

        await store.DeleteLive(new ListingQuery { ItemId = 2, WorldId = 93 });
        result = await store.GetMinListing(93, 2);
        Assert.Null(result.World.Nq);
        Assert.Null(result.World.Hq);
        Assert.Null(result.Dc.Nq);
        Assert.Null(result.Dc.Hq);
        Assert.Null(result.Region.Nq);
        Assert.Null(result.Region.Hq);
    }

#if DEBUG
    [Fact]
#endif
    public async Task GetMinListingInDcOrRegion_Works()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, 2);
        await store.ReplaceLive(currentlyShown.Listings);
        var dc = await store.GetMinListingForDcOrRegion("Gaia", 2);
        var region = await store.GetMinListingForDcOrRegion("Japan", 2);

        Assert.Equal(dc?.Nq?.UnitPrice, region?.Nq?.UnitPrice);
        Assert.Equal(dc?.Hq?.UnitPrice, region?.Hq?.UnitPrice);
    }

#if DEBUG
    [Fact]
#endif
    public async Task GetMinListing_WorksWithMultipleWorlds()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        // create 2 Listings in Gaia, 1 in Chaos, 1 in Light
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(92, 2);
        await store.ReplaceLive(currentlyShown.Listings);
        currentlyShown = SeedDataGenerator.MakeCurrentlyShown(93, 2);
        // make sure 93 is cheaper than 92
        currentlyShown.Listings.First(l => !l.Hq).PricePerUnit = 50;
        currentlyShown.Listings.First(l => l.Hq).PricePerUnit = 60;
        await store.ReplaceLive(currentlyShown.Listings);

        currentlyShown = SeedDataGenerator.MakeCurrentlyShown(39, 2);
        await store.ReplaceLive(currentlyShown.Listings);
        currentlyShown = SeedDataGenerator.MakeCurrentlyShown(36, 2);
        // make sure 36 is cheaper than 39
        currentlyShown.Listings.First(l => !l.Hq).PricePerUnit = 30;
        currentlyShown.Listings.First(l => l.Hq).PricePerUnit = 40;
        await store.ReplaceLive(currentlyShown.Listings);

        // create unrelated data
        currentlyShown = SeedDataGenerator.MakeCurrentlyShown(36, 3);
        currentlyShown.Listings.First(l => !l.Hq).PricePerUnit = 10;
        currentlyShown.Listings.First(l => l.Hq).PricePerUnit = 20;
        await store.ReplaceLive(currentlyShown.Listings);

        var result = await store.GetMinListing(92, 2);
        Assert.True(100 <= result?.World?.Nq?.UnitPrice);
        Assert.True(100 <= result?.World?.Hq?.UnitPrice);
        Assert.Equal(50, result?.Dc?.Nq?.UnitPrice);
        Assert.Equal(60, result?.Dc?.Hq?.UnitPrice);
        Assert.Equal(50, result?.Region?.Nq?.UnitPrice);
        Assert.Equal(60, result?.Region?.Hq?.UnitPrice);

        result = await store.GetMinListing(39, 2);
        Assert.True(100 <= result?.World?.Nq?.UnitPrice);
        Assert.True(100 <= result?.World?.Hq?.UnitPrice);
        Assert.Equal(result?.World?.Nq?.UnitPrice, result?.Dc?.Nq?.UnitPrice);
        Assert.Equal(result?.World?.Hq?.UnitPrice, result?.Dc?.Hq?.UnitPrice);
        Assert.Equal(30, result?.Region?.Nq?.UnitPrice);
        Assert.Equal(40, result?.Region?.Hq?.UnitPrice);
    }

    private static void AssertEqual(Listing expected, Listing actual)
    {
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
        Assert.Null(actual.CreatorId);
        Assert.Equal(expected.CreatorName, actual.CreatorName);
        Assert.Equal(new DateTimeOffset(expected.LastReviewTime).ToUnixTimeSeconds(),
            new DateTimeOffset(actual.LastReviewTime).ToUnixTimeSeconds());
        Assert.Equal(DateTimeKind.Utc, actual.LastReviewTime.Kind);
        Assert.Null(actual.SellerId);
        Assert.Equal(DateTimeKind.Utc, actual.UpdatedAt.Kind);
        Assert.Equal(expected.Source, actual.Source);
    }
}