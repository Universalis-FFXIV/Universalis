using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IMarketItemStore
{
    Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default);

    Task Update(MarketItem marketItem, CancellationToken cancellationToken = default);

    Task<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default);
}