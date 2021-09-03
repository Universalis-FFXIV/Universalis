using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class TrustedSourceDbAccess : DbAccessService<TrustedSource, TrustedSourceQuery>, ITrustedSourceDbAccess
    {
        public TrustedSourceDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "trustedSources") { }

        public TrustedSourceDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "content") { }

        public async Task Increment(TrustedSourceQuery query, CancellationToken cancellationToken = default)
        {
            if (await Retrieve(query, cancellationToken) == null)
            {
                // Sources can only be added manually, so we don't create one if none exists
                return;
            }

            var updateBuilder = Builders<TrustedSource>.Update;
            var update = updateBuilder.Inc(o => o.UploadCount, 1U);
            await Collection.UpdateOneAsync(query.ToFilterDefinition(), update, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts(CancellationToken cancellationToken = default)
        {
            var cursor = await Collection.FindAsync(o => true, cancellationToken: cancellationToken);
            var results = cursor.ToEnumerable()
                .Select(o => new TrustedSourceNoApiKey
                {
                    Name = o.Name,
                    UploadCount = o.UploadCount,
                })
                .ToList();
            return results;
        }
    }
}