using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class MostRecentlyUpdatedDbAccess : DbAccessService<MostRecentlyUpdated, MostRecentlyUpdatedQuery>, IMostRecentlyUpdatedDbAccess
    {
        public static readonly int MaxItems = 200;

        public MostRecentlyUpdatedDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "mostRecentlyUpdated") { }

        public MostRecentlyUpdatedDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "mostRecentlyUpdated") { }

        public async Task Push(uint worldId, WorldItemUpload document, CancellationToken cancellationToken = default)
        {
            var query = new MostRecentlyUpdatedQuery { WorldId = worldId };
            var existing = await Retrieve(query, cancellationToken);

            if (existing == null)
            {
                await Create(new MostRecentlyUpdated
                {
                    WorldId = worldId,
                    Uploads = new List<WorldItemUpload> { document },
                }, cancellationToken);
                return;
            }

            var uploads = existing.Uploads;
            uploads.Insert(0, document);
            uploads = existing.Uploads.Take(MaxItems).ToList();
            var updateBuilder = Builders<MostRecentlyUpdated>.Update;
            var update = updateBuilder.Set(o => o.Uploads, uploads);
            await Collection.UpdateOneAsync(query.ToFilterDefinition(), update, cancellationToken: cancellationToken);
        }

        public async Task<IList<MostRecentlyUpdated>> RetrieveMany(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
        {
            return await Collection.Find(query.ToFilterDefinition())
                .ToListAsync(cancellationToken);
        }
    }
}