using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ICurrentlyShownStore
{
    Task<IList<Listing>> Insert(CurrentlyShown data, string retainedRetainerId = null, CancellationToken cancellationToken = default);

    Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default);

    Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query,
        CancellationToken cancellationToken = default);
}