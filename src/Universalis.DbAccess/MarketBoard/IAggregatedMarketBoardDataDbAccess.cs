using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IAggregatedMarketBoardDataDbAccess
{
    Task<MinListing> GetMinListing(int worldId, int itemId);

    ValueTask<IEnumerable<MarketItem>> RetrieveWorldUploadTimes(int itemId, CancellationToken cancellationToken, params int[] worldIds);

    Task<Sale> GetMostRecentSaleInWorld(int worldId, int itemId, bool hq);

    Task<Sale> GetMostRecentSaleInDatacenterOrRegion(string dcRegion, int itemId, bool hq);

    Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(string worldIdDcRegion, int itemId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}