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

    /// <summary>
    /// Captures the frames the behaviour publishes.
    ///
    /// Publishing is fire-and-forget, so a test cannot assume it has happened by
    /// the time Execute returns. Waiting is therefore expressed as "wait until this
    /// many frames have arrived since the last <see cref="Reset"/>" - a count
    /// rather than a one-shot signal, which would be satisfied forever by the first
    /// frame and silently stop waiting on every later call.
    ///
    /// The callbacks run on the publishing task, not the test thread, so the
    /// captured lists are guarded and handed out as snapshots.
    /// </summary>
    private sealed class CapturedBus
    {
        private readonly object _gate = new();
        private readonly List<ListingsAdd> _adds = [];
        private readonly List<ListingsRemove> _removes = [];
        private int _published;

        public IBus Bus { get; init; }

        public IReadOnlyList<ListingsAdd> Adds
        {
            get { lock (_gate) { return _adds.ToList(); } }
        }

        public IReadOnlyList<ListingsRemove> Removes
        {
            get { lock (_gate) { return _removes.ToList(); } }
        }

        /// <summary>Forgets everything captured so far, including the frame count.</summary>
        public void Reset()
        {
            lock (_gate)
            {
                _adds.Clear();
                _removes.Clear();
                _published = 0;
            }
        }

        /// <summary>
        /// Waits until at least <paramref name="count"/> frames have been published
        /// since the last <see cref="Reset"/>.
        /// </summary>
        public async Task WaitForPublishesAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                lock (_gate)
                {
                    if (_published >= count) return;
                }

                await Task.Delay(5);
            }

            int seen;
            lock (_gate) { seen = _published; }
            throw new TimeoutException($"expected {count} frame(s) within {timeout}; saw {seen}");
        }

        /// <summary>
        /// Waits out a grace period and asserts nothing was published. Used where the
        /// correct behaviour is silence, which no amount of waiting can confirm - so
        /// this only has to be long enough to catch a frame that should not exist.
        /// </summary>
        public async Task AssertNoPublishesAsync(TimeSpan grace)
        {
            await Task.Delay(grace);
            lock (_gate)
            {
                Assert.Empty(_adds);
                Assert.Empty(_removes);
            }
        }

        public static CapturedBus Create()
        {
            var mock = new Mock<IBus>();
            var captured = new CapturedBus { Bus = mock.Object };

            mock.Setup(b => b.Publish(It.IsAny<ListingsAdd>(), It.IsAny<CancellationToken>()))
                .Callback<ListingsAdd, CancellationToken>((m, _) =>
                {
                    lock (captured._gate)
                    {
                        captured._adds.Add(m);
                        captured._published++;
                    }
                })
                .Returns(Task.CompletedTask);

            mock.Setup(b => b.Publish(It.IsAny<ListingsRemove>(), It.IsAny<CancellationToken>()))
                .Callback<ListingsRemove, CancellationToken>((m, _) =>
                {
                    lock (captured._gate)
                    {
                        captured._removes.Add(m);
                        captured._published++;
                    }
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
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5)); // one listings/add

        bus.Reset();

        await behavior.Execute(Source, Upload(MakeListing("l1", 100)));
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5)); // one listings/remove

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
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5));

        bus.Reset();

        // l3 is gone from the board.
        await behavior.Execute(Source, Upload(
            MakeListing("l1", 100),
            MakeListing("l2", 200)));
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5)); // one listings/remove

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
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5));

        bus.Reset();

        await behavior.Execute(Source, Upload(
            MakeListing("l1", 100),
            MakeListing("l2", 200)));
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5)); // one listings/add

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
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5));

        bus.Reset();

        await behavior.Execute(Source, Upload(MakeListing("l1", 100), MakeListing("l2", 200)));

        await bus.AssertNoPublishesAsync(TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task Publish_RepricedListing_EmitsRemoveThenAdd()
    {
        // Listings compare by id, price and quantity, so a reprice is a remove
        // followed by an add.
        var bus = CapturedBus.Create();
        var behavior = CreateBehavior(bus.Bus, TimeSpan.FromMilliseconds(20));

        await behavior.Execute(Source, Upload(MakeListing("l1", 100)));
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5));

        bus.Reset();

        await behavior.Execute(Source, Upload(MakeListing("l1", 150)));
        await bus.WaitForPublishesAsync(2, TimeSpan.FromSeconds(5)); // a remove and an add

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
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5));

        bus.Reset();

        var atTheBell = Upload(MakeListing("l2", 200, retainerId: "retB"));
        atTheBell.UploaderRetainerId = "retA";

        await behavior.Execute(Source, atTheBell);

        await bus.AssertNoPublishesAsync(TimeSpan.FromMilliseconds(300));
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
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5));

        bus.Reset();

        // At retA's bell, and retC's listing has genuinely sold.
        var atTheBell = Upload(MakeListing("l2", 200, retainerId: "retB"));
        atTheBell.UploaderRetainerId = "retA";

        await behavior.Execute(Source, atTheBell);
        await bus.WaitForPublishesAsync(1, TimeSpan.FromSeconds(5)); // one listings/remove

        var removed = Assert.Single(bus.Removes);
        Assert.Equal([300], removed.Listings.Select(l => l.PricePerUnit).ToList());
    }
}
