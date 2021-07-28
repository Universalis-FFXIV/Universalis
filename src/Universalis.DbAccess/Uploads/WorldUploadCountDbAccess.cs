using MongoDB.Driver;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class WorldUploadCountDbAccess : DbAccessService<WorldUploadCount, WorldUploadCountQuery>, IWorldUploadCountDbAccess
    {
        public WorldUploadCountDbAccess() : base("universalis", "extraData") { }

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
    }
}