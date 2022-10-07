using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IRecentlyUpdatedItemsDbAccess
{
    public ValueTask<RecentlyUpdatedItems> Retrieve(CancellationToken cancellationToken = default);

    public Task Push(uint itemId, CancellationToken cancellationToken = default);
}