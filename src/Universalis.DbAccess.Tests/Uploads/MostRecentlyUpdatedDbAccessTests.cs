﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class MostRecentlyUpdatedDbAccessTests
{
    private class MockWorldItemUploadStore : IWorldItemUploadStore
    {
        private readonly Dictionary<int, double> _scores = new();
        
        public Task SetItem(int worldId, int id, double val)
        {
            _scores[id] = val;
            return Task.CompletedTask;
        }

        public Task<IList<KeyValuePair<int, double>>> GetMostRecent(int worldId, int stop = -1)
        {
            var en = _scores.OrderByDescending(s => s.Value).ToList();
            if (stop > -1)
            {
                en = en.Take(stop + 1).ToList();
            }

            return Task.FromResult((IList<KeyValuePair<int, double>>)en);
        }
        
        public Task<IList<KeyValuePair<int, double>>> GetLeastRecent(int worldId, int stop = -1)
        {
            var en = _scores.OrderBy(s => s.Value).ToList();
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
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        var output = await db.GetMostRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 10,
        });
        
        Assert.NotNull(output);
        Assert.Empty(output);
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
        
        var output = await db.GetMostRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 10,
        });
        
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(5333, output[0].ItemId);
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
        
        var output = await db.GetMostRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 10,
        });
        Assert.NotNull(output);
        
        var uploads = output.Select(u => u.ItemId).ToList();
        Assert.Contains(5, uploads);
        Assert.Contains(5333, uploads);
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
        
        var output = await db.GetMostRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 10,
        });
        
        Assert.NotNull(output);
        Assert.Equal(5333, output[0].ItemId);
        Assert.Equal(5, output[1].ItemId);
        Assert.Equal(2, output.Count);
    }
    
    [Fact]
    public async Task PushMany_TakesCount()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        for (var i = 0; i < 20; i++)
        {
            await db.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = (int)i,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
        }
        
        var output = await db.GetMostRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 10,
        });
        Assert.NotNull(output);
        Assert.Equal(10, output.Count);
    }
}