using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockSaleStatisticsDbAccess : ISaleStatisticsDbAccess
{
    public async Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(TradeVelocityQuery query, CancellationToken cancellationToken = default)
    {
        return (null, null);
    }
}
