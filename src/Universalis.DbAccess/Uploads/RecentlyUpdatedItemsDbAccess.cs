using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess
{
    public static readonly int MaxItems = 200;

    public static readonly string Key = "Universalis.RecentlyUpdated";

    private readonly IRecentlyUpdatedItemsStore _store;

    public RecentlyUpdatedItemsDbAccess(IRecentlyUpdatedItemsStore store)
    {
        _store = store;
    }

    public async Task<RecentlyUpdatedItems> Retrieve(CancellationToken cancellationToken = default)
    {
        var items = await _store.GetAllItems(Key, MaxItems - 1);
        return new RecentlyUpdatedItems
        {
            Items = items.Take(MaxItems).Select(i => i.Key).ToList(),
        };
    }

    public async Task Push(uint itemId, CancellationToken cancellationToken = default)
    {
        await _store.SetItem(Key, itemId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}