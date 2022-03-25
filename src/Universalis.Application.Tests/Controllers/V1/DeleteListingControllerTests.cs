using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Tests;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

public class DeleteListingControllerTests
{
    [Fact]
    public async Task Controller_Post_Succeeds()
    {
        var gameData = new MockGameDataProvider();
        var flaggedUploaders = new MockFlaggedUploaderDbAccess();
        var currentlyShown = new MockCurrentlyShownDbAccess();
        var trustedSources = new MockTrustedSourceDbAccess();
        var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders);

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            await trustedSources.Create(new TrustedSource
            {
                ApiKeySha512 = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key))),
            });
        }

        var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
        await currentlyShown.Create(document);

        var originalCount = document.Listings.Count;
        var toRemove = document.Listings[0];

        var result = await controller.Post(document.ItemId, document.WorldId.ToString(), key, new DeleteListingParameters
        {
            ListingId = toRemove.ListingId,
            PricePerUnit = toRemove.PricePerUnit,
            Quantity = toRemove.Quantity,
            RetainerId = toRemove.RetainerId,
            UploaderId = document.UploaderIdHash,
        });

        Assert.IsType<OkObjectResult>(result);

        Assert.Equal(originalCount - 1, document.Listings.Count);
        Assert.DoesNotContain(toRemove, document.Listings);
    }

    [Fact]
    public async Task Controller_Post_Succeeds_WhenNone()
    {
        var gameData = new MockGameDataProvider();
        var flaggedUploaders = new MockFlaggedUploaderDbAccess();
        var currentlyShown = new MockCurrentlyShownDbAccess();
        var trustedSources = new MockTrustedSourceDbAccess();
        var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders);

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            await trustedSources.Create(new TrustedSource
            {
                ApiKeySha512 = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key))),
            });
        }

        var result = await controller.Post(5333, 74.ToString(), key, new DeleteListingParameters
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
        var gameData = new MockGameDataProvider();
        var flaggedUploaders = new MockFlaggedUploaderDbAccess();
        var currentlyShown = new MockCurrentlyShownDbAccess();
        var trustedSources = new MockTrustedSourceDbAccess();
        var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders);

        var result = await controller.Post(5333, 74.ToString(), "r87uy6t7y8u65t8", new DeleteListingParameters
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
        var gameData = new MockGameDataProvider();
        var flaggedUploaders = new MockFlaggedUploaderDbAccess();
        var currentlyShown = new MockCurrentlyShownDbAccess();
        var trustedSources = new MockTrustedSourceDbAccess();
        var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders);

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            await trustedSources.Create(new TrustedSource
            {
                ApiKeySha512 = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key))),
            });
        }

        var result = await controller.Post(5333, 74.ToString(), key, new DeleteListingParameters
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
        var gameData = new MockGameDataProvider();
        var flaggedUploaders = new MockFlaggedUploaderDbAccess();
        var currentlyShown = new MockCurrentlyShownDbAccess();
        var trustedSources = new MockTrustedSourceDbAccess();
        var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders);

        const string key = "blah";
        using (var sha512 = SHA512.Create())
        {
            await trustedSources.Create(new TrustedSource
            {
                ApiKeySha512 = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key))),
            });
        }

        var result = await controller.Post(5333, 0.ToString(), key, new DeleteListingParameters
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
        var gameData = new MockGameDataProvider();
        var flaggedUploaders = new MockFlaggedUploaderDbAccess();
        var currentlyShown = new MockCurrentlyShownDbAccess();
        var trustedSources = new MockTrustedSourceDbAccess();
        var controller = new DeleteListingController(gameData, trustedSources, currentlyShown, flaggedUploaders);

        const string key = "blah";
        const string uploaderId = "ffff";
        using (var sha512 = SHA512.Create())
        {
            await trustedSources.Create(new TrustedSource
            {
                ApiKeySha512 = Util.BytesToString(sha512.ComputeHash(Encoding.UTF8.GetBytes(key))),
            });
        }

        string uploaderIdHash;
        using (var sha256 = SHA256.Create())
        {
            uploaderIdHash = Util.BytesToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(uploaderId)));
        }

        await flaggedUploaders.Create(new FlaggedUploader { UploaderIdHash = uploaderIdHash });

        var result = await controller.Post(5333, 74.ToString(), key, new DeleteListingParameters
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