﻿using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class TrustedSourceDbAccess : DbAccessService<TrustedSource, TrustedSourceQuery>, ITrustedSourceDbAccess
    {
        public TrustedSourceDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler) : base(client, throttler, Constants.DatabaseName, "trustedSources") { }

        public TrustedSourceDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler, string databaseName) : base(client, throttler, databaseName, "content") { }

        public async Task Increment(TrustedSourceQuery query)
        {
            if (await Retrieve(query) == null)
            {
                // Sources can only be added manually, so we don't create one if none exists
                return;
            }

            var updateBuilder = Builders<TrustedSource>.Update;
            var update = updateBuilder.Inc(o => o.UploadCount, 1U);
            await Throttler.AddRequest(() => Collection.UpdateOneAsync(query.ToFilterDefinition(), update));
        }

        public async Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts()
        {
            return await Throttler.AddRequest(async () =>
            {
                var cursor = await Collection.FindAsync(o => true);
                var results = cursor.ToEnumerable()
                    .Select(o => new TrustedSourceNoApiKey
                    {
                        Name = o.Name,
                        UploadCount = o.UploadCount,
                    })
                    .ToList();
                return results;
            });
        }
    }
}