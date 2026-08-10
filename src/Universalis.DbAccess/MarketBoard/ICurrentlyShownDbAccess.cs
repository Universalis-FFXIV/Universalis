using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ICurrentlyShownDbAccess
{
    public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default);
    
    public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the document and returns the listings it displaced - the board as it
    /// was immediately before this write. Returned rather than left for the caller
    /// to read back, because a separate read cannot be ordered against this write.
    /// </summary>
    public Task<IList<Listing>> Update(CurrentlyShown document, CurrentlyShownQuery query, string retainedRetainerId = null, CancellationToken cancellationToken = default);
}