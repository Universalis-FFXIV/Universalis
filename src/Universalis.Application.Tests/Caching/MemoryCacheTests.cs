using System.Threading;
using Universalis.Application.Caching;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.Application.Tests.Caching;

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
    public void Cache_Get_ReturnsNewObject()
    {
        var cache = new MemoryCache<int, Data>(1);
        var a = new Data(1);
        cache.Set(1, a);
        var b = cache.Get(1);
        Assert.False(ReferenceEquals(a, b));
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

        threads[0] = new Thread(() =>
        {
            for (var j = 0U; j < 50000U; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                cache.Set(query, new Data(1));
                cache.Get(query);
            }
        });

        threads[1] = new Thread(() =>
        {
            for (var j = 0U; j < 50000U; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                cache.Get(query);
                cache.Set(query, new Data(1));
            }
        });

        threads[2] = new Thread(() =>
        {
            for (var j = 0U; j < 50000U; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                cache.Set(query, new Data(1));
                cache.Get(query);
            }
        });

        threads[3] = new Thread(() =>
        {
            for (var j = 0U; j < 50000U; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                cache.Delete(query);
            }
        });

        threads[0].Start();
        threads[1].Start();
        threads[2].Start();
        threads[3].Start();
        Assert.True(threads[0].Join(10000));
        Assert.True(threads[1].Join(500));
        Assert.True(threads[2].Join(500));
        Assert.True(threads[3].Join(500));
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