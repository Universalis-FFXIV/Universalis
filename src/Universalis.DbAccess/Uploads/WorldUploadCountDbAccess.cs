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
        public WorldUploadCountDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "extraData") { }

        public WorldUploadCountDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "content") { }

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
            await Collection.UpdateOneAsync(query.ToFilterDefinition(), update);
        }

        public async Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts()
        {
            return await Collection.Find(o => true).ToListAsync();
        }
    }
}