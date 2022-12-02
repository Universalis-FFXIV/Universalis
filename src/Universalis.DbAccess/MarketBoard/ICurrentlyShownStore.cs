using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ICurrentlyShownStore
{
    Task<CurrentlyShown> GetData(int worldId, int itemId, CancellationToken cancellationToken = default);

    Task SetData(CurrentlyShown data, CancellationToken cancellationToken = default);
}