using MassTransit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.AccessControl;
using Universalis.Entities.MarketBoard;
using Universalis.Tests;
using Xunit;
using Listing = Universalis.Application.Uploads.Schema.Listing;
using EntityListing = Universalis.Entities.MarketBoard.Listing;

namespace Universalis.Application.Tests.Uploads.Behaviors;

/// <summary>
/// Covers the socket frames the upload path publishes.
///
/// The existing behaviour tests all pass a null <see cref="IBus"/>, so the
/// publish path short-circuits and is never exercised. These supply a bus and
/// assert on what actually reaches it.
/// </summary>
public class MarketBoardUploadPublishTests
{
    /// <summary>
    /// Wraps the in-memory mock to count reads made by the behaviour and to make
    /// the write complete asynchronously, the way a real round-trip does.
    ///
    /// Counting reads is the structural guard. The prior board must come back from
    /// the write that displaced it; any separate read of it would be unordered
    /// against that write, and when such a read lost the race the "old" listings
    /// were the ones just written, so both diffs came out empty and a real change
    /// published no frame at all.
    /// </summary>
    private sealed class RecordingCurrentlyShownDbAccess(ICurrentlyShownDbAccess inner, TimeSpan writeDelay)
        : ICurrentlyShownDbAccess
    {
        private int _retrieveCalls;

        /// <summary>Reads issued by the caller, not counting the mock's internals.</summary>
        public int RetrieveCalls => Volatile.Read(ref _retrieveCalls);

        public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _retrieveCalls);
            return inner.Retrieve(query, cancellationToken);
        }

        public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query, CancellationToken cancellationToken = default)
            => inner.RetrieveMany(query, cancellationToken);

        public async Task<IList<EntityListing>> Update(CurrentlyShown document, CurrentlyShownQuery query,
            string retainedRetainerId = null, CancellationToken cancellationToken = default)
        {
            await Task.Delay(writeDelay, cancellationToken);
            return await inner.Update(document, query, retainedRetainerId, cancellationToken);
        }
    }

    private sealed class CapturedBus
    {
        public IBus Bus { get; init; }
        public List<ListingsAdd> Adds { get; } = [];
        public List<ListingsRemove> Removes { get; } = [];

        private readonly TaskCompletionSource _anyPublish = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Publishing is fire-and-forget, so tests wait on this rather than
        /// assuming it completed by the time Execute returned.
        /// </summary>
        public async Task WaitForPublishAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_anyPublish.Task, Task.Delay(timeout));
            if (completed != _anyPublish.Task)
            {
                throw new TimeoutException($"no frame was published within {timeout}");
            }
        }

        public static CapturedBus Create()
        {
            var mock = new Mock<IBus>();
            var captured = new CapturedBus { Bus = mock.Object };

            mock.Setup(b => b.Publish(It.IsAny<ListingsAdd>(), It.IsAny<CancellationToken>()))
                .Callback<ListingsAdd, CancellationToken>((m, _) =>
                {
                    captured.Adds.Add(m);
                    captured._anyPublish.TrySetResult();
                })
                .Returns(Task.CompletedTask);

            mock.Setup(b => b.Publish(It.IsAny<ListingsRemove>(), It.IsAny<CancellationToken>()))
                .Callback<ListingsRemove, CancellationToken>((m, _) =>
                {
                    captured.Removes.Add(m);
                    captured._anyPublish.TrySetResult();
                })
                .Returns(Task.CompletedTask);

            return captured;
        }
    }

    private const int WorldId = 74;
    private const int ItemId = 5333;

    private static readonly ApiKey Source = ApiKey.FromToken("blah", "something", true);

    private static MarketBoardUploadBehavior CreateBehavior(IBus bus, TimeSpan writeDelay,
        out RecordingCurrentlyShownDbAccess db)
    {
        db = new RecordingCurrentlyShownDbAccess(new MockCurrentlyShownDbAccess(), writeDelay);
        return new MarketBoardUploadBehavior(
            db,
            new MockHistoryDbAccess(),
            new MockUploadLogDbAccess(),
            new MockGameDataProvider(),
            bus,
            new LogFixture<MarketBoardUploadBehavior>());
    }

    private static MarketBoardUploadBehavior CreateBehavior(IBus bus, TimeSpan writeDelay)
        => CreateBehavior(bus, writeDelay, out _);

    private static Listing MakeListing(string listingId, int pricePerUnit, string retainerId = "ret")
    {
        return new Listing
        {
            ListingId = listingId,
            RetainerId = retainerId,
            RetainerName = "Retainer",
            CreatorName = "",
            PricePerUnit = pricePerUnit,
            Quantity = 1,
        };
    }

    private static UploadParameters Upload(params Listing[] listings)
    {
        return new UploadParameters
        {
            WorldId = WorldId,
            ItemId = ItemId,
            UploaderId = "uploader1",
            Listings = listings.ToList(),
        };
    }

    [Fact]
    public async Task Publish_NeverReadsTheBoardBack()
    {
        // The prior board must come from the write that displaced it. A separate
        // read cannot be ordered against that write, and when it lost the race the
        // "old" listings were the ones just written: both diffs came out empty and
        // a real change was published as no frame at all. This fails if anyone
        // reintroduces a read-back, whether awaited or not.
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(20), out var db);

        await behavior.Execute(Source, Upload(MakeListing("l1", 100), MakeListing("l2", 200)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        await behavior.Execute(Source, Upload(MakeListing("l1", 100)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(200);

        Assert.Equal(0, db.RetrieveCalls);
    }

    [Fact]
    public async Task Publish_DiffsAgainstTheDisplacedBoard()
    {
        // The diff has to be taken against the board as it was *before* this upload
        // replaced it, which is exactly what the write hands back.
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(50));

        await behavior.Execute(Source, Upload(
            MakeListing("l1", 100),
            MakeListing("l2", 200),
            MakeListing("l3", 300)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        bus.Adds.Clear();
        bus.Removes.Clear();

        // l3 is gone from the board.
        await behavior.Execute(Source, Upload(
            MakeListing("l1", 100),
            MakeListing("l2", 200)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        // Listing IDs are hashed in the view, so prices identify them here.
        var removed = Assert.Single(bus.Removes);
        Assert.Equal([300], removed.Listings.Select(l => l.PricePerUnit).ToList());
        Assert.Empty(bus.Adds);
    }

    [Fact]
    public async Task Publish_EmitsAddsForNewListings()
    {
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(50));

        await behavior.Execute(Source, Upload(MakeListing("l1", 100)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        bus.Adds.Clear();
        bus.Removes.Clear();

        await behavior.Execute(Source, Upload(
            MakeListing("l1", 100),
            MakeListing("l2", 200)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        var added = Assert.Single(bus.Adds);
        Assert.Equal([200], added.Listings.Select(l => l.PricePerUnit).ToList());
        Assert.Empty(bus.Removes);
    }

    [Fact]
    public async Task Publish_EmitsNothing_WhenTheBoardHasNotMoved()
    {
        // A player opening an item whose board has not changed bumps the upload
        // time without producing a diff. No frame here is correct - this pins that
        // it stays correct once the diff is taken before the write, rather than
        // becoming a spurious add/remove pair.
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(20));

        await behavior.Execute(Source, Upload(MakeListing("l1", 100), MakeListing("l2", 200)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        bus.Adds.Clear();
        bus.Removes.Clear();

        await behavior.Execute(Source, Upload(MakeListing("l1", 100), MakeListing("l2", 200)));

        // Give any stray publish a chance to land before asserting absence.
        await Task.Delay(300);

        Assert.Empty(bus.Adds);
        Assert.Empty(bus.Removes);
    }

    [Fact]
    public async Task Publish_RepricedListing_EmitsRemoveThenAdd()
    {
        // Listings compare by id, price and quantity, so a reprice is a remove
        // followed by an add.
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(20));

        await behavior.Execute(Source, Upload(MakeListing("l1", 100)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        bus.Adds.Clear();
        bus.Removes.Clear();

        await behavior.Execute(Source, Upload(MakeListing("l1", 150)));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(200); // let the second of the two frames land

        var removed = Assert.Single(bus.Removes);
        var added = Assert.Single(bus.Adds);

        Assert.Equal(100, removed.Listings.Single().PricePerUnit);
        Assert.Equal(150, added.Listings.Single().PricePerUnit);
    }

    [Fact]
    public async Task Publish_RetainedRetainerListings_AreNotReportedAsRemoved()
    {
        // Summoning a retainer withdraws that retainer's listings from the board,
        // so an upload taken in that context is missing them through no fault of
        // the seller. They survive the scoped delete - and no removal frame may be
        // published for them either, or a subscriber would delete listings the
        // server itself kept.
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(50));

        await behavior.Execute(Source, Upload(
            MakeListing("l1", 100, retainerId: "retA"),
            MakeListing("l2", 200, retainerId: "retB")));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        bus.Adds.Clear();
        bus.Removes.Clear();

        var atTheBell = Upload(MakeListing("l2", 200, retainerId: "retB"));
        atTheBell.UploaderRetainerId = "retA";

        await behavior.Execute(Source, atTheBell);
        await Task.Delay(300);

        Assert.Empty(bus.Removes);
        Assert.Empty(bus.Adds);
    }

    [Fact]
    public async Task Publish_RemovalOnAnotherRetainer_StillReported_DuringRetainerContext()
    {
        // The retention filter must exclude only the uploader's own retainer.
        // A listing from a different retainer really is gone and must still be
        // published, or a retainer-context upload would mask genuine removals.
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(50));

        await behavior.Execute(Source, Upload(
            MakeListing("l1", 100, retainerId: "retA"),
            MakeListing("l2", 200, retainerId: "retB"),
            MakeListing("l3", 300, retainerId: "retC")));
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        bus.Adds.Clear();
        bus.Removes.Clear();

        // At retA's bell, and retC's listing has genuinely sold.
        var atTheBell = Upload(MakeListing("l2", 200, retainerId: "retB"));
        atTheBell.UploaderRetainerId = "retA";

        await behavior.Execute(Source, atTheBell);
        await bus.WaitForPublishAsync(TimeSpan.FromSeconds(5));

        var removed = Assert.Single(bus.Removes);
        Assert.Equal([300], removed.Listings.Select(l => l.PricePerUnit).ToList());
    }
}
