using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class WorldUploadCountDbAccess : DbAccessService<WorldUploadCount, WorldUploadCountQuery>, IWorldUploadCountDbAccess
    {
        public WorldUploadCountDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler) : base(client, throttler, Constants.DatabaseName, "extraData") { }

        public WorldUploadCountDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler, string databaseName) : base(client, throttler, databaseName, "content") { }

        public async Task Increment(WorldUploadCountQuery query)
        {
            if (await Retrieve(query) == null)
            {
                await Create(new WorldUploadCount
                {
                    Count = 1,
                    WorldName = query.WorldName,
                });
                return;
            }

            var updateBuilder = Builders<WorldUploadCount>.Update;
            var update = updateBuilder.Inc(o => o.Count, 1U);
            await Throttler.AddRequest(() => Collection.UpdateOneAsync(query.ToFilterDefinition(), update));
        }

        public async Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts()
        {
            return await Throttler.AddRequest(async () =>
                (await Collection.FindAsync(o => true)).ToEnumerable().ToList());
        }
    }
}