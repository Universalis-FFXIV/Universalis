using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess, IDisposable
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
        return new RecentlyUpdatedItems
            { Items = (await _store.GetAllItems(Key, MaxItems - 1)).Select(kvp => kvp.Key).Take(MaxItems).ToList() };
    }

    public async Task Push(uint itemId, CancellationToken cancellationToken = default)
    {
        var t = Convert.ToDouble(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await _store.SetItem(Key, itemId, t);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}