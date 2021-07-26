using MongoDB.Driver;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess.Queries
{
    public class TrustedSourceQuery : DbAccessQuery<TrustedSource>
    {
        public string ApiKeyHash { get; init; }

        internal override FilterDefinition<TrustedSource> ToFilterDefinition()
        {
            var filterBuilder = Builders<TrustedSource>.Filter;
            var filter = filterBuilder.Eq(o => o.ApiKeySha256, ApiKeyHash);
            return filter;
        }
    }
}