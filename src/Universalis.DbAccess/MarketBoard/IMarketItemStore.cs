using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IMarketItemStore
{
    Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default);

    ValueTask<MarketItem> Retrieve(MarketItemQuery query, CancellationToken cancellationToken = default);
    
    ValueTask<IEnumerable<MarketItem>> RetrieveMany(MarketItemManyQuery query, CancellationToken cancellationToken = default);
}