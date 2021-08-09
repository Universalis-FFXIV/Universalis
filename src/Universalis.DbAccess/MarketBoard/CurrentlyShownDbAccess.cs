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
        public CurrentlyShownDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler) : base(client, throttler, Constants.DatabaseName, "recentData") { }

        public CurrentlyShownDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler, string databaseName) : base(client, throttler, databaseName, "recentData") { }

        public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query)
        {
            return Throttler.AddRequest(async () =>
            {
                var cursor = await Collection.FindAsync(query.ToFilterDefinition());
                return cursor.ToEnumerable();
            });
        }

        public async Task<IEnumerable<WorldItemUpload>> RetrieveByUploadTime(CurrentlyShownWorldIdsQuery query, int count, UploadOrder order)
        {
            var sortBuilder = Builders<CurrentlyShown>.Sort;
            var sortDefinition = order switch
            {
                UploadOrder.MostRecent => sortBuilder.Descending(o => o.LastUploadTimeUnixMilliseconds),
                UploadOrder.LeastRecent => sortBuilder.Ascending(o => o.LastUploadTimeUnixMilliseconds),
                _ => throw new ArgumentException("The ordering scheme provided was invalid.", nameof(order)),
            };

            var projectDefinition = Builders<CurrentlyShown>.Projection
                .Include(o => o.WorldId)
                .Include(o => o.ItemId)
                .Include(o => o.LastUploadTimeUnixMilliseconds);

            return await Throttler.AddRequest(() => Collection
                .Find(query.ToFilterDefinition())
                .Project<WorldItemUpload>(projectDefinition)
                .Sort(sortDefinition)
                .Limit(count)
                .ToListAsync());
        }
    }
}