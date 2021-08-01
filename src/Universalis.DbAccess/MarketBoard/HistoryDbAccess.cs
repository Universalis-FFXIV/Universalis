using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public class HistoryDbAccess : DbAccessService<History, HistoryQuery>, IHistoryDbAccess
    {
        public HistoryDbAccess() : base("universalis", "extendedHistory") { }

        public HistoryDbAccess(string databaseName) : base(databaseName, "extendedHistory") { }

        public async Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query)
        {
            var cursor = await Collection.FindAsync(query.ToFilterDefinition());
            return cursor.ToEnumerable();
        }
    }
}