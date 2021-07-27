using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess
{
    public class HistoryDbAccess : DbAccessService<History, HistoryQuery>, IHistoryDbAccess
    {
        public HistoryDbAccess() : base("universalis", "extendedHistory") { }

        public async Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query)
        {
            var cursor = await Collection.FindAsync(query.ToFilterDefinition());
            return cursor.ToEnumerable();
        }
    }
}