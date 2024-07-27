using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class AggregatedMarketBoardDataDbAccess : IAggregatedMarketBoardDataDbAccess
{
    private readonly IListingStore _listingStore;
    private readonly ISaleStore _saleStore;
    private readonly IMarketItemStore _marketItemStore;

    public AggregatedMarketBoardDataDbAccess(IListingStore listingStore, ISaleStore saleStore, IMarketItemStore marketItemStore)
    {
        _listingStore = listingStore;
        _saleStore = saleStore;
        _marketItemStore = marketItemStore;
    }

    public Task<MinListing> GetMinListing(int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        return _listingStore.GetMinListing(worldId, itemId, cancellationToken);
    }

    public Task<MinListing.Entry> GetMinListing(string dcRegion, int itemId, CancellationToken cancellationToken = default)
    {
        return _listingStore.GetMinListingForDcOrRegion(dcRegion, itemId, cancellationToken);
    }

    public ValueTask<IEnumerable<MarketItem>> RetrieveWorldUploadTimes(int itemId, CancellationToken cancellationToken, params int[] worldIds)
    {
        return _marketItemStore.RetrieveMany(new MarketItemManyQuery { ItemIds = new[] { itemId }, WorldIds = worldIds }, cancellationToken);
    }

    public Task<RecentSale> GetMostRecentSaleInWorld(int worldId, int itemId, bool hq, CancellationToken cancellationToken = default)
    {
        return _saleStore.GetMostRecentSaleInWorld(worldId, itemId, hq, cancellationToken);
    }

    public Task<RecentSale> GetMostRecentSaleInDatacenterOrRegion(string dcRegion, int itemId, bool hq, CancellationToken cancellationToken = default)
    {
        return _saleStore.GetMostRecentSaleInDatacenterOrRegion(dcRegion, itemId, hq, cancellationToken);
    }

    public Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(string worldIdDcRegion, int itemId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        return _saleStore.RetrieveUnitTradeVelocity(worldIdDcRegion, itemId, from, to, cancellationToken);
    }
}