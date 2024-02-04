using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownDbAccess : ICurrentlyShownDbAccess
{
    private readonly ICurrentlyShownStore _store;

    public CurrentlyShownDbAccess(ICurrentlyShownStore store)
    {
        _store = store;
    }

    public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownDbAccess.Retrieve");
        return _store.Retrieve(query, cancellationToken);
    }

    public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownDbAccess.RetrieveMany");
        return _store.RetrieveMany(query, cancellationToken);
    }

    public Task Update(CurrentlyShown document, CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownDbAccess.Update");
        return _store.Insert(document, cancellationToken);
    }
}