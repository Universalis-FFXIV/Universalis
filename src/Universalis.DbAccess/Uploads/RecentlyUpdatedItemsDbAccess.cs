using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess, IDisposable
{
    public static readonly int MaxItems = 200;

    private readonly IRecentlyUpdatedItemsStore _store;

    public RecentlyUpdatedItemsDbAccess(IRecentlyUpdatedItemsStore store)
    {
        _store = store;
    }

    public async Task<RecentlyUpdatedItems> Retrieve(CancellationToken cancellationToken = default)
    {
        return new RecentlyUpdatedItems
            { Items = (await _store.GetAllItems(MaxItems - 1)).Select(kvp => kvp.Key).Take(MaxItems).ToList() };
    }

    public async Task Push(int itemId, CancellationToken cancellationToken = default)
    {
        var t = Convert.ToDouble(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await _store.SetItem(itemId, t);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}