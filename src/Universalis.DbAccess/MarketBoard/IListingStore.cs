using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IListingStore
{
    Task UpsertLive(IEnumerable<Listing> listingGroup, CancellationToken cancellationToken = default);

    Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query, CancellationToken cancellationToken = default);
}