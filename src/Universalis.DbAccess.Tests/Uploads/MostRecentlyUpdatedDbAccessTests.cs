using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class MostRecentlyUpdatedDbAccessTests : IDisposable
{
    private class MockWorldItemUploadStore : IWorldItemUploadStore
    {
        private readonly Dictionary<uint, double> _scores = new();
        
        public Task SetItem(string key, uint id, double val)
        {
            _scores[id] = val;
            return Task.CompletedTask;
        }

        public Task<IList<KeyValuePair<uint, double>>> GetAllItems(string key, int stop = -1)
        {
            var en = _scores.OrderByDescending(s => s.Value).ToList();
            if (stop > -1)
            {
                en = en.Take(stop + 1).ToList();
            }

            return Task.FromResult((IList<KeyValuePair<uint, double>>)en);
        }
    }
    
    private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(MostRecentlyUpdatedDbAccessTests));

    private readonly IMongoClient _client;

    public MostRecentlyUpdatedDbAccessTests()
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
    public async Task Retrieve_DoesNotThrow()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        var output = await db.Retrieve(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
        });
        
        Assert.NotNull(output);
        Assert.Empty(output.Uploads);
    }

    [Fact]
    public async Task Push_DoesNotThrow()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        await db.Push(74, new WorldItemUpload
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
    }

    [Fact]
    public async Task Push_DoesRetrieve()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        await db.Push(74, new WorldItemUpload
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        
        var output = await db.Retrieve(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
        });
        
        Assert.NotNull(output);
        Assert.Single(output.Uploads);
        Assert.Equal(5333U, output.Uploads[0].ItemId);
    }

    [Fact]
    public async Task PushTwice_DoesRetrieve()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        await db.Push(74, new WorldItemUpload
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        
        await db.Push(74, new WorldItemUpload
        {
            WorldId = 74,
            ItemId = 5,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        
        var output = await db.Retrieve(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
        });
        Assert.NotNull(output);
        
        var uploads = output.Uploads.Select(u => u.ItemId).ToList();
        Assert.Contains(5U, uploads);
        Assert.Contains(5333U, uploads);
    }

    [Fact]
    public async Task PushSameTwice_DoesReorder()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        await db.Push(74, new WorldItemUpload
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        
        await db.Push(74, new WorldItemUpload
        {
            WorldId = 74,
            ItemId = 5,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        
        await db.Push(74, new WorldItemUpload
        {
            WorldId = 74,
            ItemId = 5333,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
        
        var output = await db.Retrieve(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
        });
        
        Assert.NotNull(output);
        Assert.Equal(5333U, output.Uploads[0].ItemId);
        Assert.Equal(5U, output.Uploads[1].ItemId);
        Assert.Equal(2, output.Uploads.Count);
    }
    
    [Fact]
    public async Task PushMany_TakesMax()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        for (var i = 0; i < RecentlyUpdatedItemsDbAccess.MaxItems * 2; i++)
        {
            await db.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = (uint)i,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
        }
        
        var output = await db.Retrieve(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
        });
        Assert.NotNull(output);
        Assert.Equal(RecentlyUpdatedItemsDbAccess.MaxItems, output.Uploads.Count);
    }
}