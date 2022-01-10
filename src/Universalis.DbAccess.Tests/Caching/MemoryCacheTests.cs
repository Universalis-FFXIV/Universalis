using System.Threading;
using Universalis.DbAccess.Caching;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.Caching;

public class MemoryCacheTests
{
    [Fact]
    public void Cache_DoesEviction()
    {
        var cache = new MemoryCache<int, object>(4);
        for (var i = 0; i < 5; i++)
        {
            cache.Set(i, 1);
        }

        var hits = 0;
        for (var i = 0; i < 5; i++)
        {
            var j = cache.Get(i);
            if (j is 1) hits++;
        }

        Assert.Equal(hits, cache.Capacity);
    }

    [Fact]
    public void Cache_IsThreadSafe()
    {
        var cache = new MemoryCache<CurrentlyShownQuery, object>(1);

        var threads = new Thread[4];
        for (var i = 0; i < threads.Length; i++)
        {
            var curI = i;
            threads[i] = new Thread(() =>
            {
                for (var j = curI * 50000; j < (curI + 1) * 50000; j++)
                {
                    var query = new CurrentlyShownQuery { ItemId = (uint)j, WorldId = 0 };
                    cache.Get(query);
                    cache.Set(query, 1);
                }
            });

            threads[i].Start();
        }

        threads[0].Join(10000);
        threads[1].Join(500);
        threads[2].Join(500);
        threads[3].Join(500);
    }
}