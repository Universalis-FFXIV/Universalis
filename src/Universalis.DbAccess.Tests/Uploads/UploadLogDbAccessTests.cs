using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class UploadLogDbAccessTests
{
    private sealed class FakeStore : IUploadLogStore
    {
        private readonly ConcurrentQueue<IReadOnlyList<UploadLogEntry>> _batches = new();
        private readonly Func<IReadOnlyCollection<UploadLogEntry>, Task> _onLogActions;

        public FakeStore(Func<IReadOnlyCollection<UploadLogEntry>, Task> onLogActions = null)
        {
            _onLogActions = onLogActions;
        }

        public IReadOnlyList<IReadOnlyList<UploadLogEntry>> Batches => _batches.ToList();

        public Task LogAction(UploadLogEntry entry) => Task.CompletedTask;

        public Task LogActions(IReadOnlyCollection<UploadLogEntry> entries, CancellationToken cancellationToken = default)
        {
            _batches.Enqueue(entries.ToList());
            return _onLogActions?.Invoke(entries) ?? Task.CompletedTask;
        }
    }

    private static UploadLogEntry MakeEntry(int itemId) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = DateTime.UtcNow,
        Event = "MarketBoardUpload",
        Application = "test",
        WorldId = 74,
        ItemId = itemId,
        Listings = 1,
        Sales = 0,
    };

    [Fact]
    public async Task DrainOnceAsync_FlushesAllEnqueuedEntries_AsSingleBatch()
    {
        var store = new FakeStore();
        var sut = new UploadLogDbAccess(store, NullLogger<UploadLogDbAccess>.Instance);

        await sut.LogAction(MakeEntry(1));
        await sut.LogAction(MakeEntry(2));
        await sut.LogAction(MakeEntry(3));

        await sut.DrainOnceAsync();

        var batch = Assert.Single(store.Batches);
        Assert.Equal(new[] { 1, 2, 3 }, batch.Select(e => e.ItemId));
    }

    [Fact]
    public async Task DrainOnceAsync_EmptyChannel_DoesNotCallStore()
    {
        var store = new FakeStore();
        var sut = new UploadLogDbAccess(store, NullLogger<UploadLogDbAccess>.Instance);

        await sut.DrainOnceAsync();

        Assert.Empty(store.Batches);
    }

    [Fact]
    public async Task DrainOnceAsync_RespectsMaxBatchSize()
    {
        var store = new FakeStore();
        var sut = new UploadLogDbAccess(
            store,
            NullLogger<UploadLogDbAccess>.Instance,
            capacity: 100,
            maxBatchSize: 5);

        for (var i = 0; i < 12; i++)
        {
            await sut.LogAction(MakeEntry(i));
        }

        await sut.DrainOnceAsync();
        await sut.DrainOnceAsync();
        await sut.DrainOnceAsync();

        Assert.Equal(3, store.Batches.Count);
        Assert.Equal(5, store.Batches[0].Count);
        Assert.Equal(5, store.Batches[1].Count);
        Assert.Equal(2, store.Batches[2].Count);
    }

    [Fact]
    public async Task LogAction_DropsOldest_WhenChannelFull()
    {
        var store = new FakeStore();
        var sut = new UploadLogDbAccess(
            store,
            NullLogger<UploadLogDbAccess>.Instance,
            capacity: 3,
            maxBatchSize: 100);

        // Five entries with capacity 3 — first two get dropped.
        for (var i = 1; i <= 5; i++)
        {
            await sut.LogAction(MakeEntry(i));
        }

        await sut.DrainOnceAsync();

        var batch = Assert.Single(store.Batches);
        Assert.Equal(3, batch.Count);
        Assert.Equal(new[] { 3, 4, 5 }, batch.Select(e => e.ItemId));
    }

    [Fact]
    public async Task DrainOnceAsync_StoreThrows_DoesNotPropagate()
    {
        var store = new FakeStore(_ => throw new InvalidOperationException("boom"));
        var sut = new UploadLogDbAccess(store, NullLogger<UploadLogDbAccess>.Instance);

        await sut.LogAction(MakeEntry(1));

        await sut.DrainOnceAsync();
    }

    [Fact]
    public async Task HostedServiceLifecycle_DrainsRemainingEntries_OnStop()
    {
        var store = new FakeStore();
        var sut = new UploadLogDbAccess(store, NullLogger<UploadLogDbAccess>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await sut.LogAction(MakeEntry(1));
        await sut.LogAction(MakeEntry(2));
        await sut.StopAsync(CancellationToken.None);

        var totalEntries = store.Batches.Sum(b => b.Count);
        Assert.Equal(2, totalEntries);
    }
}
