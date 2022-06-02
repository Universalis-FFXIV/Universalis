using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess
{
    public static readonly int MaxItems = 200;

    public static readonly string Key = "Universalis.RecentlyUpdated";

    private readonly IConnectionMultiplexer _redis;

    public RecentlyUpdatedItemsDbAccess(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<RecentlyUpdatedItems> Retrieve(RecentlyUpdatedItemsQuery query, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var items = await db.SortedSetRangeByScoreAsync(Key, order: Order.Descending, take: MaxItems);
        return new RecentlyUpdatedItems
        {
            Items = items.Select(i => (uint)i).ToList(),
        };
    }

    public async Task Push(uint itemId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.SortedSetAddAsync(Key, new[] { new SortedSetEntry(itemId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) });

        var count = await db.SortedSetLengthAsync(Key);
        if (count > MaxItems)
        {
            await db.SortedSetRemoveRangeByRankAsync(Key, 0, count - MaxItems);
        }
    }
}