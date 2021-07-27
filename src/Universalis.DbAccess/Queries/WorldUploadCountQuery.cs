using MongoDB.Driver;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess.Queries
{
    public class WorldUploadCountQuery : DbAccessQuery<WorldUploadCount>
    {
        public const string SetName = "worldUploadCount";

        public string WorldName { get; init; }

        internal override FilterDefinition<WorldUploadCount> ToFilterDefinition()
        {
            var filterBuilder = Builders<WorldUploadCount>.Filter;
            var filter = filterBuilder.Eq(o => o.SetName, SetName) & filterBuilder.Eq(o => o.WorldName, WorldName);
            return filter;
        }
    }
}