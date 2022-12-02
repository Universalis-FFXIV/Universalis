using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Realtime;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Schema;
using Universalis.Common.Caching;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Tests;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.Entities.Uploads;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

public class DeleteListingControllerTests
{
    private class TestResources
    {
        public IGameDataProvider GameData { get; private init; }
        public IFlaggedUploaderDbAccess FlaggedUploaders { get; private init; }
        public ICurrentlyShownDbAccess CurrentlyShown { get; private init; }
        public ITrustedSourceDbAccess TrustedSources { get; private init; }
        public LogFixture<SocketProcessor> SocketLogFixture { get; private init; }
        public ISocketProcessor Sockets { get; private init; }
        public DeleteListingController Controller { get; private init; }

        public static TestResources Create()
        {
            var gameData = new MockGameDataProvider();
            var flaggedUploaders = new MockFlaggedUploaderDbAccess();
            var currentlyShown = new MockCurrentlyShownDbAccess();
            var trustedSources = new MockTrustedSourceDbAccess();
            var socketLogFixture = new LogFixture<SocketProcessor>();
            var sockets = new SocketProcessor(socketLogFixture);
            var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders, sockets);
            return new TestResources
            {
                GameData = gameData,
                FlaggedUploaders = flaggedUploaders,
                CurrentlyShown = currentlyShown,
                TrustedSources = trustedSources,
                Controller = controller,
                SocketLogFixture = socketLogFixture,
                Sockets = sockets,
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
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
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
    public async Task Controller_Post_Succeeds_WhenNone()
    {
        var test = TestResources.Create();

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
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
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
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
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
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
            var hash = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key)));
            await test.TrustedSources.Create(new ApiKey(hash, "something", true));
        }

        string uploaderIdHash;
        using (var sha256 = SHA256.Create())
        {
            uploaderIdHash = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(uploaderId)));
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
}