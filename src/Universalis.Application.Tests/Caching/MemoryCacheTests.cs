using System.Threading.Tasks;
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
    public async Task Cache_Delete_DoesRemove()
    {
        var cache = new MemoryCache<int, Data>(1);
        await cache.Set(1, new Data(1));
        var j = await cache.Get(1);
        Assert.True(j?.Value ==  1);
        await cache.Delete(1);
        var k = await cache.Get(1);
        Assert.True(k == null);
    }

    [Fact]
    public async Task Cache_Get_ReturnsNewObject()
    {
        var cache = new MemoryCache<int, Data>(1);
        var a = new Data(1);
        cache.Set(1, a);
        var b = await cache.Get(1);
        Assert.False(ReferenceEquals(a, b));
    }

    [Fact]
    public async Task Cache_DoesEviction()
    {
        var cache = new MemoryCache<int, Data>(4);
        for (var i = 0; i < 5; i++)
        {
            cache.Set(i, new Data(1));
        }

        var hits = 0;
        for (var i = 0; i < 5; i++)
        {
            var j = await cache.Get(i);
            if (j?.Value == 1) hits++;
        }

        Assert.Equal(4, hits);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(50000)]
    public async Task Cache_IsThreadSafe(int cacheSize)
    {
        const uint nIterations = 1000000U;

        var cache = new MemoryCache<CurrentlyShownQuery, Data>(cacheSize);

        var tasks = new Task[4];

        tasks[0] = new Task(async () =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                await cache.Set(query, new Data(j));
            }
        });

        tasks[1] = new Task(async () =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                var cached = await cache.Get(query);
                if (cached != null)
                {
                    Assert.Equal(j, cached.Value);
                }
            }
        });

        tasks[2] = new Task(async () =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                await cache.Set(query, new Data(j));
            }
        });

        tasks[3] = new Task(async () =>
        {
            for (var j = 0U; j < nIterations; j++)
            {
                var query = new CurrentlyShownQuery { ItemId = j, WorldId = 0 };
                await cache.Delete(query);
            }
        });

        tasks[0].Start();
        tasks[1].Start();
        tasks[2].Start();
        tasks[3].Start();
        await tasks[0];
        await tasks[1];
        await tasks[2];
        await tasks[3];
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