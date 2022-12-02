using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockRecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess
{
    private readonly RecentlyUpdatedItems document = new() { Items = new List<int>()};

    public Task<RecentlyUpdatedItems> Retrieve(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(document);
    }

    public async Task Push(int itemId, CancellationToken cancellationToken = default)
    {
        var existing = await Retrieve(cancellationToken);
        var newItems = existing.Items;
        newItems.Insert(0, itemId);
        newItems = existing.Items.Take(RecentlyUpdatedItemsDbAccess.MaxItems).ToList();
        existing.Items = newItems;
    }
}