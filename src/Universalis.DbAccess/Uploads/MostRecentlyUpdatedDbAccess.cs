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

        private readonly AutoResetEvent _reset = new(true);

        public MostRecentlyUpdatedDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "mostRecentlyUpdated") { }

        public MostRecentlyUpdatedDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "mostRecentlyUpdated") { }

        public async Task Push(uint worldId, WorldItemUpload document, CancellationToken cancellationToken = default)
        {
            _reset.WaitOne();
            _reset.Reset();
            try
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
                var existingIndex = uploads.FindIndex(o => o.ItemId == document.ItemId);
                if (existingIndex != -1)
                {
                    uploads.RemoveAt(existingIndex);
                    uploads.Insert(0, document);
                }
                else
                {
                    uploads.Insert(0, document);
                    uploads = uploads.Take(MaxItems).ToList();
                }

                var updateBuilder = Builders<MostRecentlyUpdated>.Update;
                var update = updateBuilder.Set(o => o.Uploads, uploads);
                await Collection.UpdateOneAsync(query.ToFilterDefinition(), update,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _reset.Set();
            }
        }

        public async Task<IList<MostRecentlyUpdated>> RetrieveMany(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
        {
            return await Collection.Find(query.ToFilterDefinition())
                .ToListAsync(cancellationToken);
        }
    }
}