using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities;
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
    public async Task RetrieveLive_CachedCollectionMutationDoesNotPoisonCache()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        var currentlyShown = SeedDataGenerator.MakeCurrentlyShown(92, 22528);
        var expectedCount = currentlyShown.Listings.Count;
        await store.ReplaceLive(currentlyShown.Listings);

        var first = await store.RetrieveLive(new ListingQuery { ItemId = 22528, WorldId = 92 });
        Assert.IsType<List<Listing>>(first).AddRange(first);
        var second = await store.RetrieveLive(new ListingQuery { ItemId = 22528, WorldId = 92 });

        Assert.Equal(expectedCount, second.Count());

        var manyQuery = new ListingManyQuery { ItemIds = new[] { 22528 }, WorldIds = new[] { 92 } };
        var manyFirst = await store.RetrieveManyLive(manyQuery);
        var manyFirstListings = Assert.IsType<List<Listing>>(manyFirst[new WorldItemPair(92, 22528)]);
        manyFirstListings.AddRange(manyFirstListings);
        var manySecond = await store.RetrieveManyLive(manyQuery);

        Assert.Equal(expectedCount, manySecond[new WorldItemPair(92, 22528)].Count);
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

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_PreservesListingsAcrossWorldsWithSameListingId()
    {
        // We've observed in production that the same listing_id can appear on
        // different worlds for different retainers. Both rows should land in the
        // database. Regression test for the prior bug where a single-column PK on
        // listing_id paired with ON CONFLICT (listing_id) DO NOTHING silently
        // dropped one of the two.
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();

        const string sharedListingId = "cross-world-shared-listing-id";
        const int itemId = 999;
        const int worldA = 199;
        const int worldB = 200;

        var listingA = MakeListing(sharedListingId, worldA, itemId, pricePerUnit: 100);
        var listingB = MakeListing(sharedListingId, worldB, itemId, pricePerUnit: 200);

        await store.ReplaceLive(new List<Listing> { listingA });
        await store.ReplaceLive(new List<Listing> { listingB });

        var resultsA = (await store.RetrieveLive(new ListingQuery { ItemId = itemId, WorldId = worldA })).ToList();
        var resultsB = (await store.RetrieveLive(new ListingQuery { ItemId = itemId, WorldId = worldB })).ToList();

        Assert.Single(resultsA);
        Assert.Equal(sharedListingId, resultsA[0].ListingId);
        Assert.Equal(worldA, resultsA[0].WorldId);
        Assert.Equal(100, resultsA[0].PricePerUnit);

        Assert.Single(resultsB);
        Assert.Equal(sharedListingId, resultsB[0].ListingId);
        Assert.Equal(worldB, resultsB[0].WorldId);
        Assert.Equal(200, resultsB[0].PricePerUnit);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_EvictsOldItemCacheWhenListingItemIdChanges()
    {
        // The composite-PK upsert flips a row's item_id when the same
        // (listing_id, world_id) is uploaded under a new item_id. The local
        // listings cache for the OLD item_id must be evicted, otherwise reads
        // for that (world, oldItem) keep returning the migrated listing as if
        // it still belonged to oldItem.
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();

        const string sharedListingId = "cross-item-shared-listing-id";
        const int worldId = 197;
        const int itemA = 800;
        const int itemB = 801;

        // Seed the listing under itemA and warm the (world, itemA) cache.
        await store.ReplaceLive(new List<Listing> { MakeListing(sharedListingId, worldId, itemA, pricePerUnit: 100) });
        var resultsA1 = (await store.RetrieveLive(new ListingQuery { ItemId = itemA, WorldId = worldId })).ToList();
        Assert.Single(resultsA1);

        // Upload the same listing_id under itemB. The upsert should flip the
        // row's item_id from A to B, and ReplaceLive should evict the now-stale
        // (world, itemA) cache entry as part of the same call.
        await store.ReplaceLive(new List<Listing> { MakeListing(sharedListingId, worldId, itemB, pricePerUnit: 200) });

        var resultsA2 = (await store.RetrieveLive(new ListingQuery { ItemId = itemA, WorldId = worldId })).ToList();
        var resultsB = (await store.RetrieveLive(new ListingQuery { ItemId = itemB, WorldId = worldId })).ToList();

        Assert.Empty(resultsA2);
        Assert.Single(resultsB);
        Assert.Equal(sharedListingId, resultsB[0].ListingId);
        Assert.Equal(itemB, resultsB[0].ItemId);
        Assert.Equal(200, resultsB[0].PricePerUnit);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_Retention_MatchesPlainReplace_FinalState()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 93;
        const int itemRetention = 600;
        const int itemPlain = 601;
        const string retainerA = "ret-agg-a";
        const string retainerB = "ret-agg-b";
        const string retainerC = "ret-agg-c";

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("agg-ret-l1", world, itemRetention, 500, retainerA),
            MakeListing("agg-ret-l2", world, itemRetention, 300, retainerB),
            MakeListing("agg-ret-l3", world, itemRetention, 700, retainerC),
        });
        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("agg-pln-l1", world, itemPlain, 500, retainerA),
            MakeListing("agg-pln-l2", world, itemPlain, 300, retainerB),
            MakeListing("agg-pln-l3", world, itemPlain, 700, retainerC),
        });

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("agg-ret-l2", world, itemRetention, 300, retainerB),
            MakeListing("agg-ret-l3", world, itemRetention, 700, retainerC),
        }, retainedRetainerId: retainerA);
        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("agg-pln-l1", world, itemPlain, 500, retainerA),
            MakeListing("agg-pln-l2", world, itemPlain, 300, retainerB),
            MakeListing("agg-pln-l3", world, itemPlain, 700, retainerC),
        });

        // Identical listing sets
        var retentionResults = (await store.RetrieveLive(new ListingQuery { ItemId = itemRetention, WorldId = world }))
            .OrderBy(l => l.ListingId).ToList();
        var plainResults = (await store.RetrieveLive(new ListingQuery { ItemId = itemPlain, WorldId = world }))
            .OrderBy(l => l.ListingId).ToList();

        Assert.Equal(3, retentionResults.Count);
        Assert.Equal(3, plainResults.Count);

        // Same price distribution (sorted by price ascending)
        Assert.Equal(plainResults.Select(l => l.PricePerUnit), retentionResults.Select(l => l.PricePerUnit));

        // Same retainers (sorted by price ascending)
        Assert.Equal(plainResults.Select(l => l.RetainerId), retentionResults.Select(l => l.RetainerId));

        // Identical world min-price aggregates
        var minRetention = await store.GetMinListing(world, itemRetention);
        var minPlain = await store.GetMinListing(world, itemPlain);
        Assert.Equal(minPlain.World.Nq?.UnitPrice, minRetention.World.Nq?.UnitPrice);
        Assert.Equal(minPlain.Dc.Nq?.UnitPrice, minRetention.Dc.Nq?.UnitPrice);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_Retention_MinPriceReflectsRetainedCheapest()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 93;
        const int item = 602;
        const string retainerA = "ret-min-a";
        const string retainerB = "ret-min-b";

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("min-l1", world, item, 100, retainerA),
            MakeListing("min-l2", world, item, 500, retainerB),
        });

        // Replace with only B — A (cheapest) survives via retention
        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("min-l2", world, item, 500, retainerB),
        }, retainedRetainerId: retainerA);

        var minListing = await store.GetMinListing(world, item);
        Assert.Equal(100, minListing.World.Nq?.UnitPrice);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_Retention_HqMinPriceReflectsRetainedCheapest()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 93;
        const int item = 603;
        const string retainerA = "ret-hq-a";
        const string retainerB = "ret-hq-b";

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("hq-l1", world, item, 200, retainerA, hq: true),
            MakeListing("hq-l2", world, item, 800, retainerB, hq: true),
        });

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("hq-l2", world, item, 800, retainerB, hq: true),
        }, retainedRetainerId: retainerA);

        var minListing = await store.GetMinListing(world, item);
        Assert.Equal(200, minListing.World.Hq?.UnitPrice);
    }

    // DeleteLive with retention should preserve the retainer's rows and compute
    // aggregates from the survivors, not wipe the cache keys.
#if DEBUG
    [Fact]
#endif
    public async Task DeleteLive_Retention_PreservesRetainerAndUpdatesAggregate()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 93;
        const int item = 604;
        const string retainerA = "ret-dl-a";
        const string retainerB = "ret-dl-b";

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("dl-l1", world, item, 100, retainerA),
            MakeListing("dl-l2", world, item, 500, retainerB),
        });

        await store.DeleteLive(new ListingQuery { ItemId = item, WorldId = world }, retainedRetainerId: retainerA);

        var results = (await store.RetrieveLive(new ListingQuery { ItemId = item, WorldId = world })).ToList();
        Assert.Single(results);
        Assert.Equal(retainerA, results[0].RetainerId);

        var minListing = await store.GetMinListing(world, item);
        Assert.Equal(100, minListing.World.Nq?.UnitPrice);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_PlainReplace_AfterRetention_RemovesPreviouslyRetained()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 93;
        const int item = 605;
        const string retainerA = "ret-chain-a";
        const string retainerB = "ret-chain-b";

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("chain-l1", world, item, 100, retainerA),
            MakeListing("chain-l2", world, item, 500, retainerB),
        });

        // Retention keeps A
        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("chain-l2", world, item, 500, retainerB),
        }, retainedRetainerId: retainerA);

        var afterRetention = (await store.RetrieveLive(new ListingQuery { ItemId = item, WorldId = world })).ToList();
        Assert.Equal(2, afterRetention.Count);

        // Plain replace with only B — A is gone
        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("chain-l2", world, item, 500, retainerB),
        });

        var afterPlain = (await store.RetrieveLive(new ListingQuery { ItemId = item, WorldId = world })).ToList();
        Assert.Single(afterPlain);
        Assert.Equal(retainerB, afterPlain[0].RetainerId);
    }

    // DeleteLive must evict the local listings cache. Without the
    // eviction, a warmed cache entry survives the delete and RetrieveLive
    // serves phantom rows for up to the 5-minute TTL.
#if DEBUG
    [Fact]
#endif
    public async Task DeleteLive_EvictsLocalCache_NoRetention()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 93;
        const int item = 606;

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("evict-l1", world, item, 100),
            MakeListing("evict-l2", world, item, 500),
        });

        // Warm the local cache
        var warmed = (await store.RetrieveLive(new ListingQuery { ItemId = item, WorldId = world })).ToList();
        Assert.Equal(2, warmed.Count);

        await store.DeleteLive(new ListingQuery { ItemId = item, WorldId = world });

        // Next read must reflect the delete, not the warmed entry
        var afterDelete = (await store.RetrieveLive(new ListingQuery { ItemId = item, WorldId = world })).ToList();
        Assert.Empty(afterDelete);
    }

#if DEBUG
    [Fact]
#endif
    public async Task DeleteLive_EvictsLocalCache_WithRetention()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 93;
        const int item = 607;
        const string retainerA = "ret-evict-a";
        const string retainerB = "ret-evict-b";

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("evict-ret-l1", world, item, 100, retainerA),
            MakeListing("evict-ret-l2", world, item, 500, retainerB),
        });

        // Warm the local cache
        var warmed = (await store.RetrieveLive(new ListingQuery { ItemId = item, WorldId = world })).ToList();
        Assert.Equal(2, warmed.Count);

        await store.DeleteLive(new ListingQuery { ItemId = item, WorldId = world }, retainedRetainerId: retainerA);

        // Only the retained retainer's row survives
        var afterDelete = (await store.RetrieveLive(new ListingQuery { ItemId = item, WorldId = world })).ToList();
        Assert.Single(afterDelete);
        Assert.Equal(retainerA, afterDelete[0].RetainerId);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_ReturnsDisplacedListings()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 92;
        const int item = 30100;

        var seeded = new List<Listing>
        {
            MakeListing("displaced-l1", world, item, 100),
            MakeListing("displaced-l2", world, item, 200, hq: true),
        };
        var initial = await store.ReplaceLive(seeded);
        Assert.Empty(initial);

        var displaced = await store.ReplaceLive(new List<Listing>
        {
            MakeListing("displaced-l3", world, item, 300),
        });

        Assert.Equal(2, displaced.Count);
        Assert.All(seeded.OrderBy(l => l.PricePerUnit).Zip(displaced.OrderBy(l => l.PricePerUnit)), pair =>
        {
            var (expected, actual) = pair;
            AssertEqual(expected, actual);
        });
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_Retention_ExcludesRetainedListingsFromDisplaced()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 92;
        const int item = 30101;
        const string retainerA = "displaced-ret-a";
        const string retainerB = "displaced-ret-b";

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("ret-l1", world, item, 100, retainerA),
            MakeListing("ret-l2", world, item, 200, retainerB),
        });

        var displaced = await store.ReplaceLive(new List<Listing>
        {
            MakeListing("ret-l3", world, item, 300, retainerB),
        }, retainedRetainerId: retainerA);

        var entry = Assert.Single(displaced);
        Assert.Equal("ret-l2", entry.ListingId);
        Assert.Equal(retainerB, entry.RetainerId);
    }

#if DEBUG
    [Fact]
#endif
    public async Task DeleteLive_ReturnsRemovedListings()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 92;
        const int item = 30102;
        var query = new ListingQuery { ItemId = item, WorldId = world };

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("removed-l1", world, item, 100),
            MakeListing("removed-l2", world, item, 200),
        });

        var removed = await store.DeleteLive(query);

        Assert.Equal(new[] { "removed-l1", "removed-l2" },
            removed.Select(l => l.ListingId).OrderBy(id => id).ToArray());
        Assert.Empty(await store.RetrieveLive(query));
    }

#if DEBUG
    [Fact]
#endif
    public async Task DeleteLive_Retention_ReturnsOnlyUnretainedListings()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 92;
        const int item = 30103;
        const string retainerA = "removed-ret-a";
        const string retainerB = "removed-ret-b";
        var query = new ListingQuery { ItemId = item, WorldId = world };

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("del-l1", world, item, 100, retainerA),
            MakeListing("del-l2", world, item, 200, retainerB),
        });

        var removed = await store.DeleteLive(query, retainedRetainerId: retainerA);

        var entry = Assert.Single(removed);
        Assert.Equal("del-l2", entry.ListingId);
        Assert.Equal(retainerB, entry.RetainerId);

        var survivors = (await store.RetrieveLive(query)).ToList();
        Assert.Single(survivors);
        Assert.Equal(retainerA, survivors[0].RetainerId);
    }

#if DEBUG
    [Fact]
#endif
    public async Task ReplaceLive_MultipleGroups_UpdateEachMinCacheFromItsOwnListings()
    {
        await _fixture.ClearCache();
        var store = _fixture.Services.GetRequiredService<IListingStore>();
        const int world = 92;
        const int expensiveItem = 30104;
        const int cheapItem = 30105;

        await store.ReplaceLive(new List<Listing>
        {
            MakeListing("group-expensive", world, expensiveItem, 1000),
            MakeListing("group-cheap", world, cheapItem, 100),
        });

        var expensiveMin = await store.GetMinListing(world, expensiveItem);
        var cheapMin = await store.GetMinListing(world, cheapItem);

        Assert.Equal(1000, expensiveMin.World.Nq.UnitPrice);
        Assert.Equal(100, cheapMin.World.Nq.UnitPrice);
    }

    private static Listing MakeListing(string listingId, int worldId, int itemId, int pricePerUnit,
        string retainerId = null, bool hq = false)
    {
        var effectiveRetainerId = retainerId ?? $"retainer-{worldId}";
        return new Listing
        {
            ListingId = listingId,
            Hq = hq,
            OnMannequin = false,
            Materia = new List<Materia>(),
            PricePerUnit = pricePerUnit,
            Quantity = 1,
            DyeId = 0,
            CreatorId = "",
            CreatorName = "",
            LastReviewTime = DateTime.UtcNow,
            RetainerId = effectiveRetainerId,
            RetainerName = effectiveRetainerId,
            RetainerCityId = 1,
            SellerId = "",
            ItemId = itemId,
            WorldId = worldId,
            Source = "test runner",
        };
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
