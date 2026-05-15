using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

[Collection("Database collection")]
public class UploadLogStoreTests
{
    private readonly DbFixture _fixture;

    public UploadLogStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task LogAction_Works()
    {
        var store = _fixture.Services.GetRequiredService<IUploadLogStore>();
        var entry = new UploadLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Event = "MarketBoardUpload",
            Application = "test",
            WorldId = 74,
            ItemId = 5333,
            Listings = 3,
            Sales = 2,
        };

        await store.LogAction(entry);
    }

#if DEBUG
    [Fact]
#endif
    public async Task LogActions_BatchInsert_PersistsAllEntries()
    {
        var store = _fixture.Services.GetRequiredService<IUploadLogStore>();
        var dataSource = _fixture.Services.GetRequiredService<NpgsqlDataSource>();

        var batchTag = $"batch-{Guid.NewGuid():N}";
        var entries = new List<UploadLogEntry>
        {
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow, Event = "MarketBoardUpload", Application = batchTag, WorldId = 74, ItemId = 1, Listings = 1, Sales = 0 },
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow, Event = "ListingsUploadSuccess", Application = batchTag, WorldId = 74, ItemId = 2, Listings = 2, Sales = -1 },
            new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow, Event = "DeleteListing", Application = batchTag, WorldId = 74, ItemId = 3, Listings = 1, Sales = 0 },
        };

        await store.LogActions(entries);

        await using var command = dataSource.CreateCommand("SELECT COUNT(*) FROM upload_log WHERE application = $1");
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = batchTag });
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(entries.Count, count);
    }

#if DEBUG
    [Fact]
#endif
    public async Task LogActions_PersistsUserAgent()
    {
        var store = _fixture.Services.GetRequiredService<IUploadLogStore>();
        var dataSource = _fixture.Services.GetRequiredService<NpgsqlDataSource>();

        var batchTag = $"ua-batch-{Guid.NewGuid():N}";
        var entries = new List<UploadLogEntry>
        {
            new() { Id = Guid.CreateVersion7(), Timestamp = DateTime.UtcNow, Event = "MarketBoardUpload", Application = batchTag, WorldId = 74, ItemId = 1, Listings = 1, Sales = 0, UserAgent = "Universalis/1.0 Dalamud" },
            new() { Id = Guid.CreateVersion7(), Timestamp = DateTime.UtcNow, Event = "MarketBoardUpload", Application = batchTag, WorldId = 74, ItemId = 2, Listings = 1, Sales = 0, UserAgent = null },
        };

        await store.LogActions(entries);

        await using var command = dataSource.CreateCommand(
            "SELECT user_agent FROM upload_log WHERE application = $1 ORDER BY item_id");
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = batchTag });
        var rows = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                rows.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
            }
        }

        Assert.Equal(new[] { "Universalis/1.0 Dalamud", null }, rows);
    }

#if DEBUG
    [Fact]
#endif
    public async Task LogActions_EmptyBatch_DoesNotThrow()
    {
        var store = _fixture.Services.GetRequiredService<IUploadLogStore>();
        await store.LogActions(new List<UploadLogEntry>());
    }

#if DEBUG
    [Fact]
#endif
    public async Task LogAction_AcceptsAllEventTypes()
    {
        var store = _fixture.Services.GetRequiredService<IUploadLogStore>();
        var events = new[]
        {
            "MarketBoardUpload",
            "SalesUploadSuccess",
            "SalesUploadMalformed",
            "ListingsUploadSuccess",
            "ListingsUploadMalformed",
            "DeleteListing",
        };

        foreach (var @event in events)
        {
            await store.LogAction(new UploadLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Event = @event,
                Application = "test",
                WorldId = 74,
                ItemId = 5333,
                Listings = 1,
                Sales = 0,
            });
        }
    }
}
