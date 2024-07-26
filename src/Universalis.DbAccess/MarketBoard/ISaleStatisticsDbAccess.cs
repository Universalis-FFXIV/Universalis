using System.Threading.Tasks;
using System.Threading;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ISaleStatisticsDbAccess
{
    public Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(TradeVelocityQuery query, CancellationToken cancellationToken = default);
}
