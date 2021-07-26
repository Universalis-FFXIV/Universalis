using MongoDB.Driver;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess.Query
{
    public class TrustedSourceQuery : DbAccessQuery<TrustedSource>
    {
        public string ApiKeyHash { get; set; }

        internal override FilterDefinition<TrustedSource> ToFilterDefinition()
        {
            var filterBuilder = Builders<TrustedSource>.Filter;
            var filter = filterBuilder.Eq(o => o.ApiKeySha256, ApiKeyHash);
            return filter;
        }
    }
}