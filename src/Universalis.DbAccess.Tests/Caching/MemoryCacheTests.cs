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
    }

    [Fact]
    public void Cache_IsThreadSafe()
    {
        var cache = new MemoryCache<CurrentlyShownQuery, object>(1);

        var t1 = new Thread(() =>
        {
            for (var i = 0U; i < 50000; i++)
            {
                var query = new CurrentlyShownQuery { ItemId = i, WorldId = 0 };
                cache.Get(query);
                cache.Set(query, 1);
            }
        });

        var t2 = new Thread(() =>
        {
            for (var i = 0U; i < 50000; i++)
            {
                var query = new CurrentlyShownQuery { ItemId = i, WorldId = 0 };
                cache.Set(query, 1);
                cache.Get(query);
            }
        });

        t1.Start();
        t2.Start();

        t1.Join(10000);
        t2.Join(500);
    }
}