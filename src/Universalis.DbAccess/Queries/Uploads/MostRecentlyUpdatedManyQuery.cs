using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.Uploads
{
    public class MostRecentlyUpdatedManyQuery : DbAccessQuery<MostRecentlyUpdated>
    {
        public uint[] WorldIds { get; init; }

        internal override FilterDefinition<MostRecentlyUpdated> ToFilterDefinition()
        {
            var filterBuilder = Builders<MostRecentlyUpdated>.Filter;
            var filter = filterBuilder.In(o => o.WorldId, WorldIds);
            return filter;
        }
    }
}