using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Tests;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.Tests;
using Xunit;
using Listing = Universalis.Application.Uploads.Schema.Listing;

namespace Universalis.Application.Tests.Uploads.Behaviors;

[Collection("Database collection")]
public class MarketBoardUploadBehaviorIntegrationTests
{
    private readonly DbFixture _fixture;

    public MarketBoardUploadBehaviorIntegrationTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

    private MarketBoardUploadBehavior CreateBehavior()
    {
        return new MarketBoardUploadBehavior(
            _fixture.Services.GetRequiredService<ICurrentlyShownDbAccess>(),
            _fixture.Services.GetRequiredService<IHistoryDbAccess>(),
            _fixture.Services.GetRequiredService<IUploadLogDbAccess>(),
            new MockGameDataProvider(),
            null,
            new LogFixture<MarketBoardUploadBehavior>());
    }

    private static Listing MakeListing(string listingId, string retainerId, int pricePerUnit, int quantity = 1)
    {
        return new Listing
        {
            ListingId = listingId,
            RetainerId = retainerId,
            RetainerName = "Retainer",
            CreatorName = "",
            PricePerUnit = pricePerUnit,
            Quantity = quantity,
        };
    }

#if DEBUG
    [Fact]
#endif
    public async Task Behavior_PreservesRetainerListings_RealDb()
    {
        await _fixture.ClearCache();
        var behavior = CreateBehavior();
        var source = ApiKey.FromToken("blah", "something", true);

        var seedUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("int-l1", "retA", 1000),
                MakeListing("int-l2", "retB", 2000),
                MakeListing("int-l3", "retC", 3000),
            },
        };

        Assert.True(behavior.ShouldExecute(seedUpload));
        Assert.Null(await behavior.Execute(source, seedUpload));

        // Second upload omits retA but requests preservation
        var secondUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            UploaderRetainerId = "retA",
            Listings = new List<Listing>
            {
                MakeListing("int-l2", "retB", 2000),
                MakeListing("int-l3", "retC", 3000),
            },
        };

        Assert.True(behavior.ShouldExecute(secondUpload));
        Assert.Null(await behavior.Execute(source, secondUpload));

        var db = _fixture.Services.GetRequiredService<ICurrentlyShownDbAccess>();
        var currentlyShown = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        Assert.NotNull(currentlyShown);
        var listingIds = currentlyShown.Listings.Select(l => l.ListingId).ToList();
        Assert.Contains("int-l1", listingIds);
        Assert.Contains("int-l2", listingIds);
        Assert.Contains("int-l3", listingIds);

        // RetainerId survived the Postgres round-trip
        var retainerIds = currentlyShown.Listings.Select(l => l.RetainerId).ToList();
        Assert.Contains("retA", retainerIds);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Behavior_RemovesNonUploadedListings_RealDb()
    {
        await _fixture.ClearCache();
        var behavior = CreateBehavior();
        var source = ApiKey.FromToken("blah", "something", true);

        var seedUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("int-l4", "retA", 1000),
                MakeListing("int-l5", "retB", 2000),
            },
        };

        await behavior.Execute(source, seedUpload);

        // Second upload omits retA, no preservation
        var secondUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("int-l5", "retB", 2000),
            },
        };

        await behavior.Execute(source, secondUpload);

        var db = _fixture.Services.GetRequiredService<ICurrentlyShownDbAccess>();
        var currentlyShown = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        Assert.NotNull(currentlyShown);
        var listingIds = currentlyShown.Listings.Select(l => l.ListingId).ToList();
        Assert.DoesNotContain("int-l4", listingIds);
        Assert.Contains("int-l5", listingIds);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Behavior_RejectsUpload_WhenRetainerListingAlsoUploaded_RealDb()
    {
        await _fixture.ClearCache();
        var behavior = CreateBehavior();
        var source = ApiKey.FromToken("blah", "something", true);

        var seedUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("int-l6", "retA", 1000),
            },
        };

        await behavior.Execute(source, seedUpload);

        // Upload retA's listing while also requesting preservation — contract violation
        var badUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            UploaderRetainerId = "retA",
            Listings = new List<Listing>
            {
                MakeListing("int-l6", "retA", 1000),
            },
        };

        var result = await behavior.Execute(source, badUpload);

        Assert.NotNull(result);
        Assert.IsType<BadRequestResult>(result);

        // Existing data untouched
        var db = _fixture.Services.GetRequiredService<ICurrentlyShownDbAccess>();
        var currentlyShown = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });

        Assert.NotNull(currentlyShown);
        Assert.Single(currentlyShown.Listings);
    }
}
