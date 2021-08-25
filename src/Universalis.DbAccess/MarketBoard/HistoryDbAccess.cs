using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public class HistoryDbAccess : DbAccessService<History, HistoryQuery>, IHistoryDbAccess
    {
        public HistoryDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "extendedHistory") { }

        public HistoryDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "extendedHistory") { }

        public async Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query)
        {
            return await Collection.Find(query.ToFilterDefinition()).ToListAsync();
        }
    }
}