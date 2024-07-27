using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IAggregatedMarketBoardDataDbAccess
{
    Task<MinListing> GetMinListing(int worldId, int itemId, CancellationToken cancellationToken = default);

    Task<MinListing.Entry> GetMinListing(string dcRegion, int itemId, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<MarketItem>> RetrieveWorldUploadTimes(int itemId, CancellationToken cancellationToken, params int[] worldIds);

    Task<RecentSale> GetMostRecentSaleInWorld(int worldId, int itemId, bool hq, CancellationToken cancellationToken = default);

    Task<RecentSale> GetMostRecentSaleInDatacenterOrRegion(string dcRegion, int itemId, bool hq, CancellationToken cancellationToken = default);

    Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(string worldIdDcRegion, int itemId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}