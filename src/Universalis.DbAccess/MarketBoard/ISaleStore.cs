using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ISaleStore
{
    Task InsertMany(ICollection<Sale> sales, CancellationToken cancellationToken = default);

    Task<IEnumerable<Sale>> RetrieveBySaleTime(int worldId, int itemId, int count, DateTime? from = null, bool? hq = null,
        CancellationToken cancellationToken = default);

    Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(string worldIdDcRegion, int itemId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default);

    Task<Sale> GetMostRecentSaleInWorld(int worldId, int itemId, bool hq);

    Task<Sale> GetMostRecentSaleInDatacenterOrRegion(string dcOrRegion, int itemId, bool hq);
}