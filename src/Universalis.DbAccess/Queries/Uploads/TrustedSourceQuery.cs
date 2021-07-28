using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.Uploads
{
    public class TrustedSourceQuery : DbAccessQuery<TrustedSource>
    {
        public string ApiKeySha256 { get; init; }

        internal override FilterDefinition<TrustedSource> ToFilterDefinition()
        {
            var filterBuilder = Builders<TrustedSource>.Filter;
            var filter = filterBuilder.Eq(o => o.ApiKeySha256, ApiKeySha256);
            return filter;
        }
    }
}