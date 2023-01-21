using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class RecentlyUpdatedItemsDbAccessTests
{
    private class ScoreBoardStoreMock : IRecentlyUpdatedItemsStore
    {
        private readonly Dictionary<int, double> _scores = new();

        public Task SetItem(int id, double val)
        {
            _scores[id] = val;
            return Task.CompletedTask;
        }

        public Task<IList<KeyValuePair<int, double>>> GetAllItems(int stop = -1)
        {
            var en = _scores.OrderByDescending(s => s.Value).ToList();
            if (stop > -1)
            {
                en = en.Take(stop + 1).ToList();
            }

            return Task.FromResult((IList<KeyValuePair<int, double>>)en);
        }
    }

    [Fact]
    public async Task Retrieve_DoesNotThrow()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(new ScoreBoardStoreMock());
        var output = await db.Retrieve();
        Assert.NotNull(output);
        Assert.Empty(output.Items);
    }

    [Fact]
    public async Task Push_DoesNotThrow()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(new ScoreBoardStoreMock());
        await db.Push(5333);
    }

    [Fact]
    public async Task Push_DoesRetrieve()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(new ScoreBoardStoreMock());
        await db.Push(5333);
        var output = await db.Retrieve();
        Assert.NotNull(output);
        Assert.Single(output.Items);
        Assert.Equal(5333, output.Items[0]);
    }

    [Fact]
    public async Task PushTwice_DoesRetrieve()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(new ScoreBoardStoreMock());
        await db.Push(5333);
        await db.Push(5);
        var output = await db.Retrieve();
        Assert.NotNull(output);
        Assert.Contains(5, output.Items);
        Assert.Contains(5333, output.Items);
    }

    [Fact]
    public async Task PushSameTwice_DoesReorder()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(new ScoreBoardStoreMock());
        await db.Push(5333);
        await db.Push(5);
        await db.Push(5333);
        var output = await db.Retrieve();
        Assert.NotNull(output);
        Assert.Equal(5333, output.Items[0]);
        Assert.Equal(5, output.Items[1]);
        Assert.Equal(2, output.Items.Count);
    }

    [Fact]
    public async Task PushMany_TakesMax()
    {
        IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(new ScoreBoardStoreMock());
        for (var i = 0; i < RecentlyUpdatedItemsDbAccess.MaxItems * 2; i++)
        {
            await db.Push(i);
        }

        var output = await db.Retrieve();
        Assert.NotNull(output);
        Assert.Equal(RecentlyUpdatedItemsDbAccess.MaxItems, output.Items.Count);
    }
}