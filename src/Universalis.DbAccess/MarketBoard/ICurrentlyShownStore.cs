using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ICurrentlyShownStore
{
    Task<CurrentlyShown> GetData(uint worldId, uint itemId, CancellationToken cancellationToken = default);

    Task SetData(CurrentlyShown data, CancellationToken cancellationToken = default);
}