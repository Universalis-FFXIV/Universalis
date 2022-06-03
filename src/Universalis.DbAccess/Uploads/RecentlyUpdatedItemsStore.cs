using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Universalis.DbAccess.Uploads;

public class RecentlyUpdatedItemsStore : IScoreboardStore<uint>
{
    private readonly IConnectionMultiplexer _redis;

    public RecentlyUpdatedItemsStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SetScore(string scoreboardName, uint id, double val)
    {
        var db = _redis.GetDatabase();
        await db.SortedSetAddAsync(scoreboardName, new[] { new SortedSetEntry(id, val) });
    }

    public async Task<IList<KeyValuePair<uint, double>>> GetAllScores(string scoreboardName, int stop = -1)
    {
        var db = _redis.GetDatabase();
        var items = await db.SortedSetRangeByRankWithScoresAsync(scoreboardName, stop: stop, order: Order.Descending);
        return items.Select(i => new KeyValuePair<uint, double>((uint)i.Element, i.Score)).ToList();
    }

    public async Task TrimScores(string scoreboardName, int topKeeping)
    {
        var db = _redis.GetDatabase();
        var count = await db.SortedSetLengthAsync(scoreboardName);
        if (count > topKeeping)
        {
            await db.SortedSetRemoveRangeByRankAsync(scoreboardName, 0, count - topKeeping);
        }
    }
}