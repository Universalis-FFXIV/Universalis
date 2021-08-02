using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface IRecentlyUpdatedItemsDbAccess
    {
        public Task<RecentlyUpdatedItems> Retrieve(RecentlyUpdatedItemsQuery query);

        public Task Push(uint itemId);
    }
}