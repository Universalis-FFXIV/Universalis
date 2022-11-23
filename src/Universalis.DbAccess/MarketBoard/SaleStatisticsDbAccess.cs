using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class SaleStatisticsDbAccess : ISaleStatisticsDbAccess
{
    private readonly ISaleStore _store;

    public SaleStatisticsDbAccess(ISaleStore store)
    {
        _store = store;
    }

    public async ValueTask<long> RetrieveUnitTradeVolume(UnitTradeVolumeQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _store.RetrieveUnitTradeVolume(query.WorldId, query.ItemId, query.From, query.To, cancellationToken);
    }
}
