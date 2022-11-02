using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching.Memcached;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsStore : IRecentlyUpdatedItemsStore
{
    private static readonly string RedisKey = "Universalis.RecentlyUpdated";

    private readonly IConnectionMultiplexer _redis;

    public RecentlyUpdatedItemsStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SetItem(uint id, double val)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        await db.SortedSetAddAsync(RedisKey, new[] { new SortedSetEntry(id, val) });
    }

    public async Task<IList<KeyValuePair<uint, double>>> GetAllItems(int stop = -1)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var items = await db.SortedSetRangeByRankWithScoresAsync(RedisKey, stop: stop, order: Order.Descending);
        return items.Select(i => new KeyValuePair<uint, double>((uint)i.Element, i.Score)).ToList();
    }
}