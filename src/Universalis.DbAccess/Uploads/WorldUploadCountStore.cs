using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class WorldUploadCountStore : IWorldUploadCountStore
{
    private readonly IConnectionMultiplexer _redis;

    public WorldUploadCountStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task Increment(string key, string worldName)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        return db.SortedSetIncrementAsync(key, worldName, 1);
    }

    public async Task<IList<KeyValuePair<string, long>>> GetWorldUploadCounts(string key)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.Stats);
        var counts = await db.SortedSetRangeByRankWithScoresAsync(key, order: Order.Descending);
        return counts.Select(entry => new KeyValuePair<string, long>(entry.Element, (long)entry.Score)).ToList();
    }
}