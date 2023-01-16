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
        return _store.GetData(query.WorldId, query.ItemId, cancellationToken);
    }

    public Task Update(CurrentlyShown document, CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        return _store.SetData(document, cancellationToken);
    }
}