using System.Threading;
using Universalis.DbAccess.Caching;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.Caching;

public class MemoryCacheTests
{
    [Fact]
    public void Cache_Delete_DoesRemove()
    {
        var cache = new MemoryCache<int, Data>(1);
        cache.Set(1, new Data(1));
        var j = cache.Get(1);
        Assert.True(j?.Value ==  1);
        cache.Delete(1);
        var k = cache.Get(1);
        Assert.True(k == null);
    }

    [Fact]
    public void Cache_DoesEviction()
    {
        var cache = new MemoryCache<int, Data>(4);
        for (var i = 0; i < 5; i++)
        {
            cache.Set(i, new Data(1));
        }

        var hits = 0;
        for (var i = 0; i < 5; i++)
        {
            var j = cache.Get(i);
            if (j?.Value == 1) hits++;
        }

        Assert.Equal(hits, cache.Capacity);
    }

    [Fact]
    public void Cache_IsThreadSafe()
    {
        var cache = new MemoryCache<CurrentlyShownQuery, Data>(1);

        var threads = new Thread[4];
        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (var j = 0U; j < 50000U; j++)
                {
                    var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                    cache.Set(query, new Data(1));
                    cache.Get(query);
                }
            });

            threads[i].Start();
        }

        threads[0].Join(10000);
        threads[1].Join(500);
        threads[2].Join(500);
        threads[3].Join(500);
    }

    private class Data
    {
        public int Value { get; }

        public Data(int value)
        {
            Value = value;
        }
    }
}