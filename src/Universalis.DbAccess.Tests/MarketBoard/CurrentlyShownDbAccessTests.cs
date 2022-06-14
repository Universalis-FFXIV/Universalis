using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

public class CurrentlyShownDbAccessTests
{
    private class MockCurrentlyShownStore : ICurrentlyShownStore
    {
        private readonly Dictionary<(uint, uint), CurrentlyShown> _currentlyShown = new();
        
        public Task<CurrentlyShown> GetData(uint worldId, uint itemId)
        {
            return Task.FromResult(_currentlyShown.TryGetValue((worldId, itemId), out var data)
                ? data
                : new CurrentlyShown(0, 0, 0, "", new List<Listing>()));
        }

        public Task SetData(CurrentlyShown data)
        {
            _currentlyShown[(data.WorldId, data.ItemId)] = data;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Retrieve_Works_WhenEmpty()
    {
        var store = new MockCurrentlyShownStore();
        ICurrentlyShownDbAccess db = new CurrentlyShownDbAccess(store);

        var output = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        Assert.Null(output);
    }

    [Fact]
    public async Task Update_Retrieve_Works()
    {
        var store = new MockCurrentlyShownStore();
        ICurrentlyShownDbAccess db = new CurrentlyShownDbAccess(store);
        
        var document1 = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
        var query = new CurrentlyShownQuery { WorldId = document1.WorldId, ItemId = document1.ItemId };
        await db.Update(document1, query);

        var document2 = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
        await db.Update(document2, query);

        var retrieved = await db.Retrieve(query);
        Assert.Equal(document2.WorldId, retrieved.WorldId);
        Assert.Equal(document2.ItemId, retrieved.ItemId);
        Assert.Equal(document2.UploadSource, retrieved.UploadSource);
        Assert.Equal(document2.LastUploadTimeUnixMilliseconds, retrieved.LastUploadTimeUnixMilliseconds);
    }
}