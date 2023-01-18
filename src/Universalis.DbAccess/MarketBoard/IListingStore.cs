using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IListingStore
{
    Task UpsertLive(IEnumerable<Listing> listings, CancellationToken cancellationToken = default);

    Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query, CancellationToken cancellationToken = default);
    
    Task<IDictionary<WorldItemPair, IList<Listing>>> RetrieveManyLive(ListingManyQuery query, CancellationToken cancellationToken = default);
}