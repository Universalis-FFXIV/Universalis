using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownDbAccess : DbAccessService<CurrentlyShown, CurrentlyShownQuery>, ICurrentlyShownDbAccess
{
    public CurrentlyShownDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "recentData") { }

    public CurrentlyShownDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "recentData") { }

    public async Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(query.ToFilterDefinition()).ToListAsync(cancellationToken);
    }
}