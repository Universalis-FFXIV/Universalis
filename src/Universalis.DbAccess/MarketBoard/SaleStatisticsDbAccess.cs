using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class SaleStatisticsDbAccess : ISaleStatisticsDbAccess
{
    private readonly ISaleStore _store;

    public SaleStatisticsDbAccess(ISaleStore store)
    {
        _store = store;
    }

    public Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(TradeVelocityQuery query, CancellationToken cancellationToken = default)
    {
        return _store.RetrieveUnitTradeVelocity(query.WorldIdDcRegion, query.ItemId, query.From, query.To, cancellationToken);
    }
}
