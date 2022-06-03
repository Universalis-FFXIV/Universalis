using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class WorldItemUploadStore : IWorldItemUploadStore
{
    private readonly IConnectionMultiplexer _redis;

    public WorldItemUploadStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }
    
    public async Task SetItem(string key, uint id, double val)
    {
        var db = _redis.GetDatabase();
        await db.SortedSetAddAsync(key, new[] { new SortedSetEntry(id, val) });
    }
    
    public async Task<IList<KeyValuePair<uint, double>>> GetAllItems(string key, int stop = -1)
    {
        var db = _redis.GetDatabase();
        var items = await db.SortedSetRangeByRankWithScoresAsync(key, stop: stop, order: Order.Descending);
        return items.Select(i => new KeyValuePair<uint, double>((uint)i.Element, i.Score)).ToList();
    }
}