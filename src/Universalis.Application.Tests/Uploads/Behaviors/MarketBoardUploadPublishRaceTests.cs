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

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class MarketBoardUploadPublishRaceTests
{
    private const int WorldId = 74;
    private const int ItemId = 5333;

    [Fact]
    public async Task ConcurrentIdenticalUploads_PublishTheListingAdditionOnce()
    {
        var db = new CoordinatedCurrentlyShownDbAccess();
        var additions = new List<ListingsAdd>();
        var gate = new object();
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var publisher = new Mock<IPublishEndpoint>();
        publisher
            .Setup(endpoint => endpoint.Publish(It.IsAny<ListingsAdd>(), It.IsAny<CancellationToken>()))
            .Callback<ListingsAdd, CancellationToken>((message, _) =>
            {
                lock (gate)
                {
                    additions.Add(message);
                    published.TrySetResult();
                }
            })
            .Returns(Task.CompletedTask);
        publisher
            .Setup(endpoint => endpoint.Publish(It.IsAny<ListingsRemove>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var behavior = new MarketBoardUploadBehavior(
            db,
            new MockHistoryDbAccess(),
            new MockUploadLogDbAccess(),
            new MockGameDataProvider(),
            publisher.Object,
            new LogFixture<MarketBoardUploadBehavior>());
        var source = ApiKey.FromToken("blah", "something", true);

        var first = behavior.Execute(source, Upload());
        var second = behavior.Execute(source, Upload());
        await Task.WhenAll(first, second);
        await published.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(100);

        lock (gate)
        {
            Assert.Single(additions);
            Assert.All(additions, message =>
                Assert.Equal("listing-1", Assert.Single(message.Listings).ListingIdHash));
        }
    }

    private static UploadParameters Upload() => new()
    {
        WorldId = WorldId,
        ItemId = ItemId,
        UploaderId = "uploader-1",
        Listings = new List<Listing>
        {
            new()
            {
                ListingId = "listing-1",
                RetainerId = "retainer-1",
                RetainerName = "Retainer",
                CreatorName = "",
                PricePerUnit = 100,
                Quantity = 1,
            },
        },
    };

    private sealed class CoordinatedCurrentlyShownDbAccess : ICurrentlyShownDbAccess
    {
        private readonly object _gate = new();
        private readonly TaskCompletionSource _bothWritesStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CurrentlyShown _current;
        private int _writes;

        public Task<CurrentlyShown> Retrieve(CurrentlyShownQuery query,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The publish path must not read the board back.");

        public Task<IEnumerable<CurrentlyShown>> RetrieveMany(CurrentlyShownManyQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<CurrentlyShown>());

        public async Task<IList<Universalis.Entities.MarketBoard.Listing>> Update(CurrentlyShown document,
            CurrentlyShownQuery query, string retainedRetainerId = null,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _writes++;
                if (_writes == 2) _bothWritesStarted.TrySetResult();
            }

            await _bothWritesStarted.Task.WaitAsync(cancellationToken);
            lock (_gate)
            {
                var displaced = _current?.Listings.ToList() ?? new List<Universalis.Entities.MarketBoard.Listing>();
                _current = document;
                return displaced;
            }
        }
    }
}
