using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class WorldItemUploadStore : IWorldItemUploadStore
{
    private static readonly string KeyFormat = "Universalis.WorldItemUploadTimes.{0}";

    private readonly IPersistentRedisMultiplexer _redis;

    public WorldItemUploadStore(IPersistentRedisMultiplexer redis)
    {
        _redis = redis;
    }
    
    public async Task SetItem(int worldId, int id, double val)
    {
        using var activity = Util.ActivitySource.StartActivity("WorldItemUploadStore.SetItem");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        await db.SortedSetAddAsync(GetRedisKey(worldId), new[] { new SortedSetEntry(id, val) });
    }
    
    public async Task<IList<KeyValuePair<int, double>>> GetMostRecent(int worldId, int stop = -1)
    {
        using var activity = Util.ActivitySource.StartActivity("WorldItemUploadStore.GetMostRecent");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var items = await db.SortedSetRangeByRankWithScoresAsync(GetRedisKey(worldId), stop: stop, order: Order.Descending);
        return items.Select(i => new KeyValuePair<int, double>((int)i.Element, i.Score)).ToList();
    }
    
    public async Task<IList<KeyValuePair<int, double>>> GetLeastRecent(int worldId, int stop = -1)
    {
        using var activity = Util.ActivitySource.StartActivity("WorldItemUploadStore.GetLeastRecent");

        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var items = await db.SortedSetRangeByRankWithScoresAsync(GetRedisKey(worldId), stop: stop, order: Order.Ascending);
        return items.Select(i => new KeyValuePair<int, double>((int)i.Element, i.Score)).ToList();
    }

    private static string GetRedisKey(int worldId)
    {
        return string.Format(KeyFormat, worldId);
    }
}