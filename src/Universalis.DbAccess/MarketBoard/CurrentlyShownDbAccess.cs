using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public class CurrentlyShownDbAccess : DbAccessService<CurrentlyShown, CurrentlyShownQuery>, ICurrentlyShownDbAccess
    {
        public CurrentlyShownDbAccess() : base("universalis", "recentData") { }

        public async Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query)
        {
            var cursor = await Collection.FindAsync(query.ToFilterDefinition());
            return cursor.ToEnumerable();
        }
    }
}