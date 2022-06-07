using System.Threading;
using Universalis.Application.Caching;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.Application.Tests.Caching;

public class MemoryCacheTests
{
    [Fact]
    public void Cache_KeyEqualityAssumptionCheck()
    {
        var q1 = new CurrentlyShownQuery { ItemId = 1, WorldId = 0 };
        var q2 = new CurrentlyShownQuery { ItemId = 1, WorldId = 0 };
        Assert.Equal(q1, q2);

        var q3 = new CurrentlyShownQuery { ItemId = 1, WorldId = 2 };
        var q4 = new CurrentlyShownQuery { ItemId = 1, WorldId = 3 };
        Assert.NotEqual(q3, q4);

        var q5 = new CurrentlyShownQuery { ItemId = 2, WorldId = 1 };
        var q6 = new CurrentlyShownQuery { ItemId = 3, WorldId = 1 };
        Assert.NotEqual(q5, q6);
    }

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

        Assert.Equal(hits, 4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(50000)]
    public void Cache_IsThreadSafe(int cacheSize)
    {
        const uint nIterations = 1000000U;

        var cache = new MemoryCache<CurrentlyShownQuery, Data>(cacheSize);

        var threads = new Thread[4];

        threads[0] = new Thread(() =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                cache.Set(query, new Data(j));
            }
        });

        threads[1] = new Thread(() =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                var cached = cache.Get(query);
                if (cached != null)
                {
                    Assert.Equal(j, cached.Value);
                }
            }
        });

        threads[2] = new Thread(() =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                cache.Set(query, new Data(j));
            }
        });

        threads[3] = new Thread(() =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                cache.Delete(query);
            }
        });

        threads[0].Start();
        threads[1].Start();
        threads[2].Start();
        threads[3].Start();
        Assert.True(threads[0].Join(30000));
        Assert.True(threads[1].Join(1000));
        Assert.True(threads[2].Join(1000));
        Assert.True(threads[3].Join(1000));
    }

    private class Data
    {
        public uint Value { get; }

        public Data(uint value)
        {
            Value = value;
        }
    }
}