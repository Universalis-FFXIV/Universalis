using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Common.Caching;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Caching;

public class CurrentlyShownCache : MemoryCache<CachedCurrentlyShownQuery, CachedCurrentlyShownData>
{
    private readonly ICurrentlyShownDbAccess _currentlyShownDb;
    private readonly IHistoryDbAccess _historyDb;

    public CurrentlyShownCache(int size, ICurrentlyShownDbAccess currentlyShownDb, IHistoryDbAccess historyDb) :
        base(size)
    {
        _currentlyShownDb = currentlyShownDb;
        _historyDb = historyDb;
    }

    public override async ValueTask<CachedCurrentlyShownData> Get(CachedCurrentlyShownQuery key,
        CancellationToken cancellationToken = default)
    {

        // Retrieve data from the database
        var currentData = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = key.WorldId,
            ItemId = key.ItemId,
        }, cancellationToken);
        var history = await _historyDb.Retrieve(new HistoryQuery
        {
            WorldId = key.WorldId,
            ItemId = key.ItemId,
            Count = 20,
        }, cancellationToken);

        if (currentData == null || history == null)
        {
            return null;
        }

        // Transform data into a view
        var dataConversions = await Task.WhenAll((currentData.Listings ?? new List<Listing>())
            .Select(l => Util.ListingToView(l, cancellationToken)));
        var dataListings = dataConversions
            .Where(s => s.PricePerUnit > 0)
            .Where(s => s.Quantity > 0)
            .ToList();

        var dataHistory = (history.Sales ?? new List<Sale>())
            .Where(s => s.PricePerUnit > 0)
            .Where(s => s.Quantity > 0)
            .Where(s => new DateTimeOffset(s.SaleTime).ToUnixTimeSeconds() > 0)
            .Select(Util.SaleToView)
            .ToList();

        var dataView = new CachedCurrentlyShownData
        {
            ItemId = key.ItemId,
            WorldId = key.WorldId,
            LastUploadTimeUnixMilliseconds = Math.Max(Convert.ToInt64(history.LastUploadTimeUnixMilliseconds), currentData.LastUploadTimeUnixMilliseconds),
            Listings = dataListings,
            RecentHistory = dataHistory,
        };

        return dataView;
    }

    public override ValueTask Set(CachedCurrentlyShownQuery key, CachedCurrentlyShownData value,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask<bool> Delete(CachedCurrentlyShownQuery key,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(false);
    }
}