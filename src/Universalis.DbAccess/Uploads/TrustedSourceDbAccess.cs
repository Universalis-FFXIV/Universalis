using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class TrustedSourceDbAccess : DbAccessService<TrustedSource, TrustedSourceQuery>, ITrustedSourceDbAccess
    {
        public TrustedSourceDbAccess() : base("universalis", "trustedSources") { }

        public async Task Increment(TrustedSourceQuery query)
        {
            if (await Retrieve(query) == null)
            {
                // Sources can only be added manually, so we don't create one if none exists
                return;
            }

            var updateBuilder = MongoDB.Driver.Builders<TrustedSource>.Update;
            var update = updateBuilder.Inc(o => o.UploadCount, 1U);
            await Collection.UpdateOneAsync(query.ToFilterDefinition(), update);
        }
    }
}