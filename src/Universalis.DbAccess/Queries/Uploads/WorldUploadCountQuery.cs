using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.Uploads
{
    public class WorldUploadCountQuery : DbAccessQuery<WorldUploadCount>
    {
        public string WorldName { get; init; }

        internal override FilterDefinition<WorldUploadCount> ToFilterDefinition()
        {
            var filterBuilder = Builders<WorldUploadCount>.Filter;
            var filter = filterBuilder.Eq(o => o.SetName, WorldUploadCount.DefaultSetName) & filterBuilder.Eq(o => o.WorldName, WorldName);
            return filter;
        }
    }
}