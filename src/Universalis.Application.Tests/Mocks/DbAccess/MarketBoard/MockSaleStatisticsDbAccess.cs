using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockSaleStatisticsDbAccess : ISaleStatisticsDbAccess
{
    public ValueTask<long> RetrieveGilTradeVolume(TradeVolumeQuery query, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<long>(100);
    }

    public ValueTask<long> RetrieveUnitTradeVolume(TradeVolumeQuery query, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<long>(100);
    }
}
