using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsStore : IRecentlyUpdatedItemsStore
{
    private static readonly string RedisKey = "Universalis.RecentlyUpdated";

    private readonly IPersistentRedisMultiplexer _redis;

    public RecentlyUpdatedItemsStore(IPersistentRedisMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SetItem(int id, double val)
    {
        using var activity = Util.ActivitySource.StartActivity("RecentlyUpdatedItemsStore.SetItem");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        await db.SortedSetAddAsync(RedisKey, new[] { new SortedSetEntry(id, val) }, CommandFlags.FireAndForget);
    }

    public async Task<IList<KeyValuePair<int, double>>> GetAllItems(int stop = -1)
    {
        using var activity = Util.ActivitySource.StartActivity("RecentlyUpdatedItemsStore.GetAllItems");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var items = await db.SortedSetRangeByRankWithScoresAsync(RedisKey, stop: stop, order: Order.Descending);
        return items.Select(i => new KeyValuePair<int, double>((int)i.Element, i.Score)).ToList();
    }
}