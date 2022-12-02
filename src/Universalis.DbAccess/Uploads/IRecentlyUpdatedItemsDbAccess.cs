using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IRecentlyUpdatedItemsDbAccess
{
    public Task<RecentlyUpdatedItems> Retrieve(CancellationToken cancellationToken = default);

    public Task Push(int itemId, CancellationToken cancellationToken = default);
}