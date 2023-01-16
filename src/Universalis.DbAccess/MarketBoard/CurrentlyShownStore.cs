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

    public async Task<CurrentlyShown> GetData(int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.GetData");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        var result = await FetchData(db, worldId, itemId, cancellationToken);
        return result ?? new CurrentlyShown();
    }

    public async Task SetData(CurrentlyShown data, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.SetData");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        await StoreData(db, data, null, cancellationToken);
    }

    private async Task StoreData(IDatabaseAsync db, CurrentlyShown data, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.StoreData");

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
        await SetLastUpdated(db, worldId, itemId, lastUploadTime, expiry);
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

    private async Task<CurrentlyShown> FetchData(IDatabaseAsync db, int worldId, int itemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.FetchData");

        var lastUpdated = await EnsureLastUpdated(db, worldId, itemId);
        if (lastUpdated.IsNullOrEmpty || (long)lastUpdated == 0)
        {
            return null;
        }

        // Attempt to retrieve listings from Postgres
        List<Listing> listings;
        try
        {
            var listingsEnumerable = await _listingStore.RetrieveLive(
                new ListingQuery { ItemId = itemId, WorldId = worldId },
                cancellationToken);
            listings = listingsEnumerable.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings from primary database (item={ItemId}, world={WorldId})",
                itemId, worldId);
            throw;
        }

        var guess = listings.FirstOrDefault();
        var guessUploadTime = guess == null ? 0 : new DateTimeOffset(guess.UpdatedAt).ToUnixTimeMilliseconds();
        return new CurrentlyShown
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = Math.Max(guessUploadTime, (long)lastUpdated),
            UploadSource = guess?.Source ?? "",
            Listings = listings,
        };
    }

    private static string GetLastUpdatedKey(int worldId, int itemId)
    {
        return $"{worldId}:{itemId}:LastUpdated";
    }
}