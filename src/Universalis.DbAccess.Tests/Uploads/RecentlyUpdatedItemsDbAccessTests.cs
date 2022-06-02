using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class RecentlyUpdatedItemsDbAccessTests : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
        
    public RecentlyUpdatedItemsDbAccessTests()
    {
        _redis = ConnectionMultiplexer.Connect("localhost:6379");
        _redis.GetDatabase().KeyDelete(RecentlyUpdatedItemsDbAccess.Key);
    }

    public void Dispose()
    {
        _redis.GetDatabase().KeyDelete(RecentlyUpdatedItemsDbAccess.Key);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Retrieve_DoesNotThrow()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(_redis);
        var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
        Assert.NotNull(output);
        Assert.Empty(output.Items);
    }

    [Fact]
    public async Task Push_DoesNotThrow()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(_redis);
        await db.Push(5333);
    }

    [Fact]
    public async Task Push_DoesRetrieve()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(_redis);
        await db.Push(5333);
        var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
        Assert.NotNull(output);
        Assert.Single(output.Items);
        Assert.Equal(5333U, output.Items[0]);
    }

    [Fact]
    public async Task PushTwice_DoesRetrieve()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(_redis);
        await db.Push(5333);
        await db.Push(5);
        var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
        Assert.NotNull(output);
        Assert.Equal(5U, output.Items[0]);
        Assert.Equal(5333U, output.Items[1]);
    }

    [Fact]
    public async Task PushSameTwice_DoesReorder()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(_redis);
        await db.Push(5333);
        await db.Push(5);
        await db.Push(5333);
        var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
        Assert.NotNull(output);
        Assert.Equal(5333U, output.Items[0]);
        Assert.Equal(5U, output.Items[1]);
        Assert.Equal(2, output.Items.Count);
    }
    
    [Fact]
    public async Task PushMany_TakesMax()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(_redis);
        for (var i = 0; i < RecentlyUpdatedItemsDbAccess.MaxItems * 2; i++)
        {
            await db.Push((uint)i);
        }
        
        var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
        Assert.NotNull(output);
        Assert.Equal((uint)RecentlyUpdatedItemsDbAccess.MaxItems, output.Items[^1]);
        Assert.Equal((uint)RecentlyUpdatedItemsDbAccess.MaxItems + 1, output.Items[^2]);
        Assert.Equal(RecentlyUpdatedItemsDbAccess.MaxItems, output.Items.Count);
    }
}