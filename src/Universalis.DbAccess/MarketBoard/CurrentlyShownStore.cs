using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownStore : ICurrentlyShownStore
{
    private readonly IWorldItemUploadStore _worldItemUploadStore;
    private readonly IListingStore _listingStore;
    private readonly ILogger<CurrentlyShownStore> _logger;

    public CurrentlyShownStore(IWorldItemUploadStore worldItemUploadStore,
        IListingStore listingStore, ILogger<CurrentlyShownStore> logger)
    {
        _worldItemUploadStore = worldItemUploadStore;
        _listingStore = listingStore;
        _logger = logger;
    }

    public async Task Insert(CurrentlyShown data, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.Insert");

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
        await SetLastUpdated(worldId, itemId, lastUploadTime);
    }

    public async Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.Retrieve");

        var lastUpdated = await GetLastUpdated(query.WorldId, query.ItemId);
        if (lastUpdated == 0)
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
            LastUploadTimeUnixMilliseconds = Math.Max(guessUploadTime, lastUpdated),
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
        var worldItemPairs = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => new WorldItemPair(worldId, itemId)))
            .ToList();

        // Get all update times from Redis
        var lastUpdatedByItem = new Dictionary<WorldItemPair, long>();
        var lastUpdatedTasks =
            await Task.WhenAll(worldItemPairs.Select(t => GetLastUpdated(t.WorldId, t.ItemId)));
        foreach (var (key, lastUpdated) in worldItemPairs.Zip(lastUpdatedTasks))
        {
            lastUpdatedByItem[key] = lastUpdated;
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

        return worldItemPairs
            .Select(key =>
            {
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
                    WorldId = key.WorldId,
                    ItemId = key.ItemId,
                    LastUploadTimeUnixMilliseconds = Math.Max(guessUploadTime, lastUpdated),
                    UploadSource = guess?.Source ?? "",
                    // I don't remember why/if this needs to be a concrete type but I
                    // think this has a fast path internally anyways.
                    Listings = listings.ToList(),
                };
            })
            .Where(cs => cs is not null);
    }

    private async Task<long> GetLastUpdated(int worldId, int itemId)
    {
        var timestamp = await _worldItemUploadStore.GetUploadTime(worldId, itemId);
        return !timestamp.HasValue ? 0 : Convert.ToInt64(timestamp.Value);
    }

    private Task SetLastUpdated(int worldId, int itemId, long timestamp)
    {
        return _worldItemUploadStore.SetItem(worldId, itemId, timestamp);
    }
}