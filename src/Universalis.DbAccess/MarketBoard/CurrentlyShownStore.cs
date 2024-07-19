using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        activity?.AddTag("worldId", data.WorldId);
        activity?.AddTag("itemId", data.ItemId);
        activity?.AddTag("source", data.UploadSource);
        activity?.AddTag("listings", data.Listings.Count);

        var worldId = data.WorldId;
        var itemId = data.ItemId;
        var uploadSource = data.UploadSource;
        var lastUploadTime = data.LastUploadTimeUnixMilliseconds;
        var listings = data.Listings;

        if (listings.Any())
        {
            await _listingStore.ReplaceLive(listings.Select(l =>
            {
                l.ItemId = itemId;
                l.WorldId = worldId;
                l.Source = uploadSource;
                return l;
            }), cancellationToken);
        }
        else
        {
            await _listingStore.DeleteLive(new ListingQuery { ItemId = itemId, WorldId = worldId }, cancellationToken);
        }

        await SetLastUpdated(worldId, itemId, lastUploadTime);
    }

    public async Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownStore.Retrieve");

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

        // It's more efficient to reuse the upload time from the listing itself than to query another store for that information
        var guess = listings.FirstOrDefault();
        var guessUploadTime = guess == null ? 0 : new DateTimeOffset(guess.UpdatedAt).ToUnixTimeMilliseconds();
        return new CurrentlyShown
        {
            WorldId = query.WorldId,
            ItemId = query.ItemId,
            LastUploadTimeUnixMilliseconds = guessUploadTime,
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

        // Attempt to retrieve listings from Postgres
        activity?.AddEvent(new ActivityEvent("GetListings"));
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
                var listings = listingsByItem[key];

                // It's more efficient to reuse the upload time from the listing itself than to query another store for that information
                var guess = listings.FirstOrDefault();
                var guessUploadTime = guess == null ? 0 : new DateTimeOffset(guess.UpdatedAt).ToUnixTimeMilliseconds();
                return new CurrentlyShown
                {
                    WorldId = key.WorldId,
                    ItemId = key.ItemId,
                    LastUploadTimeUnixMilliseconds = guessUploadTime,
                    UploadSource = guess?.Source ?? "",
                    // I don't remember why/if this needs to be a concrete type, but I
                    // think this has a fast path internally anyway.
                    Listings = listings.ToList(),
                };
            });
    }

    private Task SetLastUpdated(int worldId, int itemId, long timestamp)
    {
        return _worldItemUploadStore.SetItem(worldId, itemId, timestamp);
    }
}