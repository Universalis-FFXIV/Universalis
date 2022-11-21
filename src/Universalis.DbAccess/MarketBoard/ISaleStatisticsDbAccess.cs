using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Universalis.DbAccess.Queries.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ISaleStatisticsDbAccess
{
    public Task<IDictionary<SaleVolumeWindow, long>> RetrieveUnitTradeVolume(UnitTradeVolumeQuery query,
        CancellationToken cancellationToken = default);
}
