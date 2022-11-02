using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class WorldItemUploadStore : IWorldItemUploadStore
{
    private static readonly string KeyFormat = "Universalis.WorldItemUploadTimes.{0}";

    private readonly IConnectionMultiplexer _redis;

    public WorldItemUploadStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }
    
    public async Task SetItem(uint worldId, uint id, double val)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        await db.SortedSetAddAsync(GetRedisKey(worldId), new[] { new SortedSetEntry(id, val) });
    }
    
    public async Task<IList<KeyValuePair<uint, double>>> GetMostRecent(uint worldId, int stop = -1)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var items = await db.SortedSetRangeByRankWithScoresAsync(GetRedisKey(worldId), stop: stop, order: Order.Descending);
        return items.Select(i => new KeyValuePair<uint, double>((uint)i.Element, i.Score)).ToList();
    }
    
    public async Task<IList<KeyValuePair<uint, double>>> GetLeastRecent(uint worldId, int stop = -1)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var items = await db.SortedSetRangeByRankWithScoresAsync(GetRedisKey(worldId), stop: stop, order: Order.Ascending);
        return items.Select(i => new KeyValuePair<uint, double>((uint)i.Element, i.Score)).ToList();
    }

    private static string GetRedisKey(uint worldId)
    {
        return string.Format(KeyFormat, worldId);
    }
}