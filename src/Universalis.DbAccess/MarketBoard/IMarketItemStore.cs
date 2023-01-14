using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IMarketItemStore
{
    Task SetData(MarketItem marketItem, CancellationToken cancellationToken = default);

    ValueTask<MarketItem> GetData(int worldId, int itemId, CancellationToken cancellationToken = default);
}