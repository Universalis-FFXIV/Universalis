using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IListingStore
{
    Task DeleteLive(ListingQuery query, CancellationToken cancellationToken = default);

    Task ReplaceLive(ICollection<Listing> listings, CancellationToken cancellationToken = default);

    Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query, CancellationToken cancellationToken = default);

    Task<IDictionary<WorldItemPair, IList<Listing>>> RetrieveManyLive(ListingManyQuery query, CancellationToken cancellationToken = default);

    Task<MinListing> GetMinListing(int worldId, int itemId, CancellationToken cancellationToken = default);

    Task<MinListing.Entry> GetMinListingForDcOrRegion(string dcOrRegion, int itemId, CancellationToken cancellationToken = default);
}