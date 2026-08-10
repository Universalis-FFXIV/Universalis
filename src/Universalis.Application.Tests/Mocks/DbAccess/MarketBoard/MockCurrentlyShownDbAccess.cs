using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockCurrentlyShownDbAccess : ICurrentlyShownDbAccess
{
    private readonly List<CurrentlyShown> _collection = new();

    private Task Create(CurrentlyShown document)
    {
        _collection.Add(document);
        return Task.CompletedTask;
    }

    public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_collection
            .FirstOrDefault(d => d.WorldId == query.WorldId && d.ItemId == query.ItemId));
    }

    public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_collection.Where(d =>
            query.WorldIds.Contains(d.WorldId) && query.ItemIds.Contains(d.ItemId)));
    }

    public async Task<IList<Listing>> Update(CurrentlyShown document, CurrentlyShownQuery query,
        string retainedRetainerId = null, CancellationToken cancellationToken = default)
    {
        var existing = await Retrieve(query, cancellationToken);
        var existingListings = existing?.Listings ?? new List<Listing>();

        // Mirrors the scoped delete in ListingStore: rows belonging to the retained
        // retainer survive the write, so they are neither displaced nor returned.
        var kept = string.IsNullOrEmpty(retainedRetainerId)
            ? new List<Listing>()
            : existingListings
                .Where(l => l.RetainerId == retainedRetainerId)
                .Where(l => !document.Listings.Any(nl => nl.ListingId == l.ListingId))
                .ToList();

        IList<Listing> replaced = existingListings.Except(kept).ToList();

        document.Listings.AddRange(kept);

        await Delete(query, cancellationToken);
        await Create(document);

        return replaced;
    }

    private async Task Delete(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        var document = await Retrieve(query, cancellationToken);
        _collection.Remove(document);
    }
}
