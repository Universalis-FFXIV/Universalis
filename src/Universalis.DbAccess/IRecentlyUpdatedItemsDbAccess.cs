using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess
{
    public interface IRecentlyUpdatedItemsDbAccess
    {
        public Task Create(RecentlyUpdatedItems document);

        public Task<RecentlyUpdatedItems> Retrieve(RecentlyUpdatedItemsQuery query);

        public Task Update(RecentlyUpdatedItems document, RecentlyUpdatedItemsQuery query);

        public Task Push(uint itemId);

        public Task Delete(RecentlyUpdatedItemsQuery query);
    }
}