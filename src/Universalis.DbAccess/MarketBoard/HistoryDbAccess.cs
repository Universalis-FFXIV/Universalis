using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public class HistoryDbAccess : DbAccessService<History, HistoryQuery>, IHistoryDbAccess
    {
        public HistoryDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler) : base(client, throttler, Constants.DatabaseName, "extendedHistory") { }

        public HistoryDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler, string databaseName) : base(client, throttler, databaseName, "extendedHistory") { }

        public Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query)
        {
            return Throttler.AddRequest(async () =>
            {
                var cursor = await Collection.FindAsync(query.ToFilterDefinition());
                return cursor.ToEnumerable();
            });
        }
    }
}