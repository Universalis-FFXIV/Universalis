using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class RecentlyUpdatedItemsQuery : DbAccessQuery<RecentlyUpdatedItems>
{
    internal override FilterDefinition<RecentlyUpdatedItems> ToFilterDefinition()
    {
        var filterBuilder = Builders<RecentlyUpdatedItems>.Filter;
        var filter = filterBuilder.Eq(o => o.SetName, RecentlyUpdatedItems.DefaultSetName);
        return filter;
    }
}