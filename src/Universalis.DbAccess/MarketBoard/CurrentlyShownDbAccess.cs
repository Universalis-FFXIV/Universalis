using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public class CurrentlyShownDbAccess : DbAccessService<CurrentlyShown, CurrentlyShownQuery>, ICurrentlyShownDbAccess
    {
        public CurrentlyShownDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "recentData") { }

        public CurrentlyShownDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "recentData") { }

        public async Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query)
        {
            var cursor = await Collection.FindAsync(query.ToFilterDefinition());
            return cursor.ToEnumerable();
        }

        public async Task<IEnumerable<WorldItemUpload>> RetrieveByUploadTime(CurrentlyShownWorldIdsQuery query, int count, UploadOrder order)
        {
            var sortBuilder = Builders<CurrentlyShown>.Sort;
            var sortDefinition = order switch
            {
                UploadOrder.MostRecent => sortBuilder.Descending(o => o.LastUploadTimeUnixMilliseconds),
                UploadOrder.LeastRecent => sortBuilder.Ascending(o => o.LastUploadTimeUnixMilliseconds),
                _ => throw new ArgumentException(nameof(order)),
            };

            var projectDefinition = Builders<CurrentlyShown>.Projection
                .Include(o => o.WorldId)
                .Include(o => o.ItemId)
                .Include(o => o.LastUploadTimeUnixMilliseconds);

            return await Collection
                .Find(query.ToFilterDefinition())
                .Project<WorldItemUpload>(projectDefinition)
                .Sort(sortDefinition)
                .Limit(count)
                .ToListAsync();
        }
    }
}