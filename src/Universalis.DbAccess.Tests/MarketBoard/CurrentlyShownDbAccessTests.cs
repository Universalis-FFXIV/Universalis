using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard;

public class CurrentlyShownDbAccessTests : IDisposable
{
    private class MockCurrentlyShownStore : ICurrentlyShownStore
    {
        private readonly Dictionary<(uint, uint), CurrentlyShownSimple> _currentlyShown = new();
        
        public Task<CurrentlyShownSimple> GetData(uint worldId, uint itemId)
        {
            if (_currentlyShown.TryGetValue((worldId, itemId), out var data))
            {
                return Task.FromResult(data);
            }

            return Task.FromResult(new CurrentlyShownSimple(0, 0, 0, "", new List<ListingSimple>(),
                new List<SaleSimple>()));
        }

        public Task SetData(CurrentlyShownSimple data)
        {
            _currentlyShown[(data.WorldId, data.ItemId)] = data;
            return Task.CompletedTask;
        }
    }
    
    private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(CurrentlyShownDbAccessTests));

    private readonly IMongoClient _client;

    public CurrentlyShownDbAccessTests()
    {
        _client = new MongoClient("mongodb://localhost:27017");
        _client.DropDatabase(Database);
    }

    public void Dispose()
    {
        _client.DropDatabase(Database);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Retrieve_Works_WhenEmpty()
    {
        var store = new MockCurrentlyShownStore();
        ICurrentlyShownDbAccess db = new CurrentlyShownDbAccess(_client, Database, store);

        var output = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        Assert.Null(output);
    }

    [Fact]
    public async Task Update_Retrieve_Works()
    {
        var store = new MockCurrentlyShownStore();
        ICurrentlyShownDbAccess db = new CurrentlyShownDbAccess(_client, Database, store);
        
        var document1 = SeedDataGenerator.MakeCurrentlyShownSimple(74, 5333);
        var query = new CurrentlyShownQuery { WorldId = document1.WorldId, ItemId = document1.ItemId };
        await db.Update(document1, query);

        var document2 = SeedDataGenerator.MakeCurrentlyShownSimple(74, 5333);
        await db.Update(document2, query);

        var retrieved = await db.Retrieve(query);
        Assert.Equal(document2.WorldId, retrieved.WorldId);
        Assert.Equal(document2.ItemId, retrieved.ItemId);
        Assert.Equal(document2.UploadSource, retrieved.UploadSource);
        Assert.Equal(document2.LastUploadTimeUnixMilliseconds, retrieved.LastUploadTimeUnixMilliseconds);
    }
}