using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.Uploads;

public class MostRecentlyUpdatedQuery : DbAccessQuery<MostRecentlyUpdated>
{
    public uint WorldId { get; init; }

    internal override FilterDefinition<MostRecentlyUpdated> ToFilterDefinition()
    {
        var filterBuilder = Builders<MostRecentlyUpdated>.Filter;
        var filter = filterBuilder.Eq(o => o.WorldId, WorldId);
        return filter;
    }
}