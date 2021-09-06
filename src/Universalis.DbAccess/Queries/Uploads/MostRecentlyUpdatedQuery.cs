using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.Uploads
{
    public class MostRecentlyUpdatedQuery : DbAccessQuery<WorldItemUpload>
    {
        internal override FilterDefinition<WorldItemUpload> ToFilterDefinition()
        {
            var filterBuilder = Builders<WorldItemUpload>.Filter;
            var filter = filterBuilder.Empty;
            return filter;
        }
    }
}