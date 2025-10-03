using System;
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
        private readonly Dictionary<(int worldId, int itemId), double> _scores = new();

        public Task SetItem(int worldId, int itemId, double val)
        {
            _scores[(worldId, itemId)] = val;
            return Task.CompletedTask;
        }

        public Task<IList<KeyValuePair<int, double>>> GetMostRecent(int worldId, int stop = -1)
        {
            var en = _scores
                .Where(kvp => kvp.Key.worldId == worldId)
                .Select(kvp => new KeyValuePair<int, double>(kvp.Key.itemId, kvp.Value))
                .OrderByDescending(s => s.Value)
                .ToList();
            if (stop > -1)
            {
                en = en.Take(stop + 1).ToList();
            }

            return Task.FromResult((IList<KeyValuePair<int, double>>)en);
        }

        public Task<IList<KeyValuePair<int, double>>> GetLeastRecent(int worldId, int stop = -1)
        {
            var en = _scores
                .Where(kvp => kvp.Key.worldId == worldId)
                .Select(kvp => new KeyValuePair<int, double>(kvp.Key.itemId, kvp.Value))
                .OrderBy(s => s.Value)
                .ToList();
            if (stop > -1)
            {
                en = en.Take(stop + 1).ToList();
            }

            return Task.FromResult((IList<KeyValuePair<int, double>>)en);
        }

        public Task<double?> GetUploadTime(int worldId, int itemId)
        {
            return Task.FromResult<double?>(
                _scores.TryGetValue((worldId, itemId), out var timestamp) ? timestamp : null);
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
                ItemId = i,
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

    [Fact]
    public async Task GetAllMostRecent_EmptyWorlds_ReturnsEmpty()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        var output = await db.GetAllMostRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 10,
        });

        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task GetAllMostRecent_MultipleWorlds_ReturnsMostRecent()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = baseTime });
        await db.Push(74,
            new WorldItemUpload { WorldId = 74, ItemId = 2, LastUploadTimeUnixMilliseconds = baseTime + 1000 });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 3, LastUploadTimeUnixMilliseconds = baseTime + 2000 });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 4, LastUploadTimeUnixMilliseconds = baseTime + 3000 });

        var output = await db.GetAllMostRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 3,
        });

        Assert.NotNull(output);
        Assert.Equal(3, output.Count);
        Assert.Equal(4, output[0].ItemId);
        Assert.Equal(75, output[0].WorldId);
        Assert.Equal(3, output[1].ItemId);
        Assert.Equal(75, output[1].WorldId);
        Assert.Equal(2, output[2].ItemId);
        Assert.Equal(74, output[2].WorldId);
    }

    [Fact]
    public async Task GetAllMostRecent_CountExceedsItems_ReturnsAll()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = baseTime });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 2, LastUploadTimeUnixMilliseconds = baseTime + 1000 });

        var output = await db.GetAllMostRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 10,
        });

        Assert.NotNull(output);
        Assert.Equal(2, output.Count);
    }

    [Fact]
    public async Task GetLeastRecent_EmptyWorld_ReturnsEmpty()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        var output = await db.GetLeastRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 10,
        });

        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task GetLeastRecent_MultipleItems_ReturnsLeastRecent()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = baseTime });
        await db.Push(74,
            new WorldItemUpload { WorldId = 74, ItemId = 2, LastUploadTimeUnixMilliseconds = baseTime + 1000 });
        await db.Push(74,
            new WorldItemUpload { WorldId = 74, ItemId = 3, LastUploadTimeUnixMilliseconds = baseTime + 2000 });

        var output = await db.GetLeastRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 2,
        });

        Assert.NotNull(output);
        Assert.Equal(2, output.Count);
        Assert.Equal(1, output[0].ItemId);
        Assert.Equal(2, output[1].ItemId);
    }

    [Fact]
    public async Task GetLeastRecent_TakesCount()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        for (var i = 0; i < 20; i++)
        {
            await db.Push(74, new WorldItemUpload
            {
                WorldId = 74,
                ItemId = i,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + i,
            });
        }

        var output = await db.GetLeastRecent(new MostRecentlyUpdatedQuery
        {
            WorldId = 74,
            Count = 5,
        });

        Assert.NotNull(output);
        Assert.Equal(5, output.Count);
        Assert.Equal(0, output[0].ItemId);
    }

    [Fact]
    public async Task GetAllLeastRecent_EmptyWorlds_ReturnsEmpty()
    {
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(new MockWorldItemUploadStore());
        var output = await db.GetAllLeastRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 10,
        });

        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task GetAllLeastRecent_MultipleWorlds_ReturnsLeastRecent()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = baseTime });
        await db.Push(74,
            new WorldItemUpload { WorldId = 74, ItemId = 2, LastUploadTimeUnixMilliseconds = baseTime + 1000 });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 3, LastUploadTimeUnixMilliseconds = baseTime + 2000 });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 4, LastUploadTimeUnixMilliseconds = baseTime + 3000 });

        var output = await db.GetAllLeastRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 3,
        });

        Assert.NotNull(output);
        Assert.Equal(3, output.Count);
        Assert.Equal(1, output[0].ItemId);
        Assert.Equal(74, output[0].WorldId);
        Assert.Equal(2, output[1].ItemId);
        Assert.Equal(74, output[1].WorldId);
        Assert.Equal(3, output[2].ItemId);
        Assert.Equal(75, output[2].WorldId);
    }

    [Fact]
    public async Task GetAllLeastRecent_CountExceedsItems_ReturnsAll()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        var baseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = baseTime });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 2, LastUploadTimeUnixMilliseconds = baseTime + 1000 });

        var output = await db.GetAllLeastRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 10,
        });

        Assert.NotNull(output);
        Assert.Equal(2, output.Count);
    }

    [Fact]
    public async Task GetAllLeastRecent_WithRealisticTimestamps_ReturnsCorrectOrder()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        // Use realistic Unix timestamps (milliseconds since epoch)
        // These large values would cause integer overflow with (int)(b - a)
        var baseTime = 1727827200000L; // Oct 2, 2024
        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = baseTime });
        await db.Push(74,
            new WorldItemUpload { WorldId = 74, ItemId = 2, LastUploadTimeUnixMilliseconds = baseTime + 5000000000L });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 3, LastUploadTimeUnixMilliseconds = baseTime + 1000000000L });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 4, LastUploadTimeUnixMilliseconds = baseTime + 3000000000L });

        var output = await db.GetAllLeastRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 4,
        });

        Assert.NotNull(output);
        Assert.Equal(4, output.Count);
        // Should be ordered by timestamp ascending (least recent first)
        Assert.Equal(1, output[0].ItemId); // baseTime
        Assert.Equal(3, output[1].ItemId); // baseTime + 1B
        Assert.Equal(4, output[2].ItemId); // baseTime + 3B
        Assert.Equal(2, output[3].ItemId); // baseTime + 5B
    }

    [Fact]
    public async Task GetAllMostRecent_WithRealisticTimestamps_ReturnsCorrectOrder()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        // Use realistic Unix timestamps (milliseconds since epoch)
        var baseTime = 1727827200000L; // Oct 2, 2024
        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = baseTime });
        await db.Push(74,
            new WorldItemUpload { WorldId = 74, ItemId = 2, LastUploadTimeUnixMilliseconds = baseTime + 5000000000L });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 3, LastUploadTimeUnixMilliseconds = baseTime + 1000000000L });
        await db.Push(75,
            new WorldItemUpload { WorldId = 75, ItemId = 4, LastUploadTimeUnixMilliseconds = baseTime + 3000000000L });

        var output = await db.GetAllMostRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 4,
        });

        Assert.NotNull(output);
        Assert.Equal(4, output.Count);
        // Should be ordered by timestamp descending (most recent first)
        Assert.Equal(2, output[0].ItemId); // baseTime + 5B
        Assert.Equal(4, output[1].ItemId); // baseTime + 3B
        Assert.Equal(3, output[2].ItemId); // baseTime + 1B
        Assert.Equal(1, output[3].ItemId); // baseTime
    }

    [Fact]
    public async Task GetAllMostRecent_WithTimestampsCausingIntOverflow_ReturnsCorrectOrder()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        // Create timestamps from different times that will cause int overflow when subtracted
        // Timestamps from Jan 2020 vs Oct 2024 - difference exceeds int.MaxValue
        var oldTime = 1577836800000.0; // Jan 1, 2020
        var newTime = 1727827200000.0; // Oct 2, 2024
        // Difference: ~150 billion, which exceeds int.MaxValue (~2.1 billion)

        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = oldTime });
        await db.Push(75, new WorldItemUpload { WorldId = 75, ItemId = 2, LastUploadTimeUnixMilliseconds = newTime });

        var output = await db.GetAllMostRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 2,
        });

        Assert.NotNull(output);
        Assert.Equal(2, output.Count);
        // Item 2 should be first (most recent)
        Assert.Equal(2, output[0].ItemId);
        Assert.Equal(1, output[1].ItemId);
    }

    [Fact]
    public async Task GetAllLeastRecent_WithTimestampsCausingIntOverflow_ReturnsCorrectOrder()
    {
        var store = new MockWorldItemUploadStore();
        IMostRecentlyUpdatedDbAccess db = new MostRecentlyUpdatedDbAccess(store);

        // Create timestamps from different times that will cause int overflow when subtracted
        var oldTime = 1577836800000.0; // Jan 1, 2020
        var newTime = 1727827200000.0; // Oct 2, 2024

        await db.Push(74, new WorldItemUpload { WorldId = 74, ItemId = 1, LastUploadTimeUnixMilliseconds = oldTime });
        await db.Push(75, new WorldItemUpload { WorldId = 75, ItemId = 2, LastUploadTimeUnixMilliseconds = newTime });

        var output = await db.GetAllLeastRecent(new MostRecentlyUpdatedManyQuery
        {
            WorldIds = new[] { 74, 75 },
            Count = 2,
        });

        Assert.NotNull(output);
        Assert.Equal(2, output.Count);
        // Item 1 should be first (least recent)
        Assert.Equal(1, output[0].ItemId);
        Assert.Equal(2, output[1].ItemId);
    }
}