using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess
{
    public static readonly int MaxItems = 200;

    public static readonly string Key = "Universalis.RecentlyUpdated";

    private readonly IScoreboardStore<uint> _store;

    public RecentlyUpdatedItemsDbAccess(IScoreboardStore<uint> store)
    {
        _store = store;
    }

    public async Task<RecentlyUpdatedItems> Retrieve(RecentlyUpdatedItemsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await _store.GetAllScores(Key, MaxItems - 1);
        return new RecentlyUpdatedItems
        {
            Items = items.Take(MaxItems).Select(i => i.Key).ToList(),
        };
    }

    public async Task Push(uint itemId, CancellationToken cancellationToken = default)
    {
        await _store.SetScore(Key, itemId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}