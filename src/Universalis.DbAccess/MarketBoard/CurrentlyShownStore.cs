using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownStore : ICurrentlyShownStore
{
    private readonly IPersistentRedisMultiplexer _redis;
    private readonly IListingStore _listingStore;
    private readonly ILogger<CurrentlyShownStore> _logger;

    public CurrentlyShownStore(IPersistentRedisMultiplexer redis,
        IListingStore listingStore, ILogger<CurrentlyShownStore> logger)
    {
        _redis = redis;
        _listingStore = listingStore;
        _logger = logger;
    }

    public async Task Insert(CurrentlyShown data, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.Insert");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);

        var worldId = data.WorldId;
        var itemId = data.ItemId;
        var uploadSource = data.UploadSource;
        var lastUploadTime = data.LastUploadTimeUnixMilliseconds;
        var listings = data.Listings;

        await _listingStore.UpsertLive(listings.Select(l =>
        {
            l.ItemId = itemId;
            l.WorldId = worldId;
            l.Source = uploadSource;
            return l;
        }), cancellationToken);
        await SetLastUpdated(db, worldId, itemId, lastUploadTime);
    }

    public async Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.Retrieve");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);

        var lastUpdated = await EnsureLastUpdated(db, query.WorldId, query.ItemId);
        if (lastUpdated.IsNullOrEmpty || (long)lastUpdated == 0)
        {
            return null;
        }

        // Attempt to retrieve listings from Postgres
        List<Listing> listings;
        try
        {
            var listingsEnumerable = await _listingStore.RetrieveLive(
                new ListingQuery { ItemId = query.ItemId, WorldId = query.WorldId },
                cancellationToken);
            listings = listingsEnumerable.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings from database (world={}, item={})",
                query.WorldId, query.ItemId);
            throw;
        }

        var guess = listings.FirstOrDefault();
        var guessUploadTime = guess == null ? 0 : new DateTimeOffset(guess.UpdatedAt).ToUnixTimeMilliseconds();
        return new CurrentlyShown
        {
            WorldId = query.WorldId,
            ItemId = query.ItemId,
            LastUploadTimeUnixMilliseconds = Math.Max(guessUploadTime, (long)lastUpdated),
            UploadSource = guess?.Source ?? "",
            Listings = listings,
        };
    }

    public async Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.RetrieveMany");

        var worldIds = query.WorldIds.ToList();
        var itemIds = query.ItemIds.ToList();
        var worldItemTuples = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => (worldId, itemId)))
            .ToList();

        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);

        // Get all update times from Redis
        var lastUpdatedByItem = new Dictionary<WorldItemPair, long>();
        var lastUpdatedTasks =
            await Task.WhenAll(worldItemTuples.Select(t => EnsureLastUpdated(db, t.worldId, t.itemId)));
        foreach (var ((worldId, itemId), lastUpdated) in worldItemTuples.Zip(lastUpdatedTasks))
        {
            var key = new WorldItemPair(worldId, itemId);
            if (lastUpdated.IsNullOrEmpty)
            {
                lastUpdatedByItem[key] = 0;
            }

            lastUpdatedByItem[key] = (long)lastUpdated;
        }

        // Attempt to retrieve listings from Postgres
        IDictionary<WorldItemPair, IList<Listing>> listingsByItem;
        try
        {
            listingsByItem = await _listingStore.RetrieveManyLive(
                new ListingManyQuery { ItemIds = query.ItemIds, WorldIds = query.WorldIds },
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings from database (worlds={}, items={})",
                string.Join(',', worldIds), string.Join(',', itemIds));
            throw;
        }

        return worldItemTuples
            .Select(t =>
            {
                var key = new WorldItemPair(t.worldId, t.itemId);

                var lastUpdated = lastUpdatedByItem[key];
                if (lastUpdated == 0)
                {
                    return null;
                }

                var listings = listingsByItem[key];

                var guess = listings.FirstOrDefault();
                var guessUploadTime = guess == null ? 0 : new DateTimeOffset(guess.UpdatedAt).ToUnixTimeMilliseconds();
                return new CurrentlyShown
                {
                    WorldId = t.worldId,
                    ItemId = t.itemId,
                    LastUploadTimeUnixMilliseconds = Math.Max(guessUploadTime, lastUpdated),
                    UploadSource = guess?.Source ?? "",
                    // I don't remember why/if this needs to be a concrete type but I
                    // think this has a fast path internally anyways.
                    Listings = listings.ToList(),
                };
            })
            .Where(cs => cs is not null);
    }

    private static async Task<RedisValue> EnsureLastUpdated(IDatabaseAsync db, int worldId, int itemId)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.EnsureLastUpdated");

        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        await db.StringSetAsync(lastUpdatedKey, 0, when: When.NotExists);
        return await db.StringGetAsync(lastUpdatedKey);
    }

    private static Task SetLastUpdated(IDatabaseAsync db, int worldId, int itemId, long timestamp,
        TimeSpan? expiry = null)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.SetLastUpdated");

        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        return db.StringSetAsync(lastUpdatedKey, timestamp, expiry);
    }

    private static string GetLastUpdatedKey(int worldId, int itemId)
    {
        return $"{worldId}:{itemId}:LastUpdated";
    }
}