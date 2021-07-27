using MongoDB.Driver;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess.Queries
{
    public class RecentlyUpdatedItemsQuery : DbAccessQuery<RecentlyUpdatedItems>
    {
        public const string SetName = "recentlyUpdated";

        internal override FilterDefinition<RecentlyUpdatedItems> ToFilterDefinition()
        {
            var filterBuilder = Builders<RecentlyUpdatedItems>.Filter;
            var filter = filterBuilder.Eq(o => o.SetName, SetName);
            return filter;
        }
    }
}