using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Tests;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.Entities.Uploads;
using Universalis.GameData;
using Universalis.Tests;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

public class DeleteListingControllerTests
{
    private const int TestWorldId = 74;
    private const int TestItemId = 5333;

    private class TestResources
    {
        public IGameDataProvider GameData { get; private init; }
        public IFlaggedUploaderDbAccess FlaggedUploaders { get; private init; }
        public ICurrentlyShownDbAccess CurrentlyShown { get; private init; }
        public ITrustedSourceDbAccess TrustedSources { get; private init; }
        public IUploadLogDbAccess UploadLog { get; private init; }
        public DeleteListingController Controller { get; private init; }

        public static TestResources Create()
        {
            return Create(null);
        }

        public static TestResources Create(IPublishEndpoint publisher)
        {
            var gameData = new MockGameDataProvider();
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var currentlyShown = new MockCurrentlyShownDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var uploadLog = new MockUploadLogDbAccess();
            var logger = new LogFixture<DeleteListingController>();
            var bus = publisher == null ? Enumerable.Empty<IPublishEndpoint>() : new[] { publisher };
            var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders, uploadLog, logger,
                bus);
            return new TestResources
            {
                GameData = gameData,
                FlaggedUploaders = flaggedUploaders,
                CurrentlyShown = currentlyShown,
                TrustedSources = trustedSources,
                UploadLog = uploadLog,
                Controller = controller,
            };
        }
    }
    
    [Fact]
    public async Task Controller_Post_Succeeds()
    {
        var test = TestResources.Create();

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
        await test.CurrentlyShown.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var originalCount = document.Listings.Count;
        var toRemove = document.Listings[0];

        var result = await test.Controller.Post(document.ItemId, document.WorldId.ToString(), key, new DeleteListingParameters
        {
            ListingId = toRemove.ListingId,
            PricePerUnit = toRemove.PricePerUnit,
            Quantity = toRemove.Quantity,
            RetainerId = toRemove.RetainerId,
            UploaderId = "FB",
        });

        var updatedDocument = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = 74,
            ItemId = 5333,
        });

        Assert.IsType<OkObjectResult>(result);

        Assert.Equal(originalCount - 1, updatedDocument.Listings.Count);

        var toRemoveIndex = updatedDocument.Listings.IndexOf(toRemove);
        Assert.Equal(-1, toRemoveIndex);
    }

    [Fact]
    public async Task Controller_Post_Logs_DeleteListing_OnSuccessfulDelete()
    {
        var test = TestResources.Create();

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
        await test.CurrentlyShown.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        var toRemove = document.Listings[0];

        await test.Controller.Post(document.ItemId, document.WorldId.ToString(), key, new DeleteListingParameters
        {
            ListingId = toRemove.ListingId,
            PricePerUnit = toRemove.PricePerUnit,
            Quantity = toRemove.Quantity,
            RetainerId = toRemove.RetainerId,
            UploaderId = "FB",
        }, userAgent: "Universalis/1.0 Dalamud");

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        var entry = Assert.Single(logged.Where(e => e.Event == "DeleteListing"));
        Assert.Equal("something", entry.Application);
        Assert.Equal(74, entry.WorldId);
        Assert.Equal(5333, entry.ItemId);
        Assert.Equal(1, entry.Listings);
        Assert.Equal(0, entry.Sales);
        Assert.Equal("Universalis/1.0 Dalamud", entry.UserAgent);
    }

    [Fact]
    public async Task Controller_Post_DoesNotLog_DeleteListing_WhenNoMatchingListing()
    {
        var test = TestResources.Create();

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        await test.Controller.Post(5333, 74.ToString(), key, new DeleteListingParameters
        {
            ListingId = "95448465132123465",
            PricePerUnit = 300,
            Quantity = 76,
            RetainerId = "84984654567658768",
            UploaderId = "ffff",
        });

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        Assert.DoesNotContain(logged, e => e.Event == "DeleteListing");
    }

    [Fact]
    public async Task Controller_Post_Succeeds_WhenNone()
    {
        var test = TestResources.Create();

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var result = await test.Controller.Post(5333, 74.ToString(), key, new DeleteListingParameters
        {
            ListingId = "95448465132123465",
            PricePerUnit = 300,
            Quantity = 76,
            RetainerId = "84984654567658768",
            UploaderId = "ffff",
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Post_Fails_WithBadAuthorizationHeader()
    {
        var test = TestResources.Create();

        var result = await test.Controller.Post(5333, 74.ToString(), "r87uy6t7y8u65t8", new DeleteListingParameters
        {
            ListingId = "95448465132123465",
            PricePerUnit = 300,
            Quantity = 76,
            RetainerId = "84984654567658768",
            UploaderId = "ffff",
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Controller_Post_Fails_WithNoUploaderId()
    {
        var test = TestResources.Create();

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var result = await test.Controller.Post(5333, 74.ToString(), key, new DeleteListingParameters
        {
            ListingId = "95448465132123465",
            PricePerUnit = 300,
            Quantity = 76,
            RetainerId = "84984654567658768",
        });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Controller_Post_Fails_WhenWorldInvalid()
    {
        var test = TestResources.Create();

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var result = await test.Controller.Post(5333, 0.ToString(), key, new DeleteListingParameters
        {
            ListingId = "95448465132123465",
            PricePerUnit = 300,
            Quantity = 76,
            RetainerId = "84984654567658768",
            UploaderId = "ffff",
        });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Controller_Post_FailsSilently_WhenFlagged()
    {
        var test = TestResources.Create();

        const string key = "blah";
        const string uploaderId = "ffff";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        string uploaderIdHash;
        using (var sha256 = SHA256.Create())
        {
            uploaderIdHash = Util.Hash(sha256, uploaderId);
        }

        await test.FlaggedUploaders.Create(new FlaggedUploader(uploaderIdHash));

        var result = await test.Controller.Post(5333, 74.ToString(), key, new DeleteListingParameters
        {
            ListingId = "95448465132123465",
            PricePerUnit = 300,
            Quantity = 76,
            RetainerId = "84984654567658768",
            UploaderId = "ffff",
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Post_PublishesRemoveEvent()
    {
        var published = new List<ListingsRemove>();
        var publisher = new Mock<IPublishEndpoint>();
        publisher
            .Setup(endpoint => endpoint.Publish(It.IsAny<ListingsRemove>(), It.IsAny<CancellationToken>()))
            .Callback<ListingsRemove, CancellationToken>((message, _) => published.Add(message))
            .Returns(Task.CompletedTask);
        var test = TestResources.Create(publisher.Object);

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        var document = SeedDataGenerator.MakeCurrentlyShown(TestWorldId, TestItemId);
        await test.CurrentlyShown.Update(document, new CurrentlyShownQuery { WorldId = TestWorldId, ItemId = TestItemId });

        var toRemove = document.Listings[0];

        var result = await test.Controller.Post(document.ItemId, document.WorldId.ToString(), key, new DeleteListingParameters
        {
            ListingId = toRemove.ListingId,
            PricePerUnit = toRemove.PricePerUnit,
            Quantity = toRemove.Quantity,
            RetainerId = toRemove.RetainerId,
            UploaderId = "FB",
        });

        Assert.IsType<OkObjectResult>(result);
        var message = Assert.Single(published);
        Assert.Equal(TestWorldId, message.WorldId);
        Assert.Equal(TestItemId, message.ItemId);
        var listing = Assert.Single(message.Listings);
        Assert.Equal(toRemove.PricePerUnit, listing.PricePerUnit);
        Assert.Equal(toRemove.Quantity, listing.Quantity);
    }

    [Fact]
    public async Task Controller_Post_DoesNotPublish_WhenNoMatchingListing()
    {
        var publisher = new Mock<IPublishEndpoint>();
        var test = TestResources.Create(publisher.Object);

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.Hash(sha512, key);
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        await test.Controller.Post(TestItemId, TestWorldId.ToString(), key, new DeleteListingParameters
        {
            ListingId = "95448465132123465",
            PricePerUnit = 300,
            Quantity = 76,
            RetainerId = "84984654567658768",
            UploaderId = "ffff",
        });

        publisher.Verify(
            endpoint => endpoint.Publish(It.IsAny<ListingsRemove>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
