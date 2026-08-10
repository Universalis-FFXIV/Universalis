using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface IListingStore
{
    /// <summary>Deletes the live listings and returns the rows removed.</summary>
    Task<IList<Listing>> DeleteLive(ListingQuery query, string retainedRetainerId = null, CancellationToken cancellationToken = default);

    /// <summary>Replaces the live listings and returns the rows displaced.</summary>
    Task<IList<Listing>> ReplaceLive(ICollection<Listing> listings, string retainedRetainerId = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query, CancellationToken cancellationToken = default);

    Task<IDictionary<WorldItemPair, IList<Listing>>> RetrieveManyLive(ListingManyQuery query, CancellationToken cancellationToken = default);

    Task<MinListing> GetMinListing(int worldId, int itemId, CancellationToken cancellationToken = default);

    Task<MinListing.Entry> GetMinListingForDcOrRegion(string dcOrRegion, int itemId, CancellationToken cancellationToken = default);

    Task<IEnumerable<MarketItem>> GetCachedUploadTime(ICollection<MarketItemQuery> queries, CancellationToken cancellationToken);
}