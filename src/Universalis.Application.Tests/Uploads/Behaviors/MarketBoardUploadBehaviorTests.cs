using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Realtime;
using System.Linq;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.DbAccess.Uploads;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Tests.Mocks.Realtime;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.GameData;
using Universalis.Tests;
using Xunit;
using Listing = Universalis.Application.Uploads.Schema.Listing;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class MarketBoardUploadBehaviorTests
{
    private class TestResources
    {
        public ICurrentlyShownDbAccess CurrentlyShown { get; private init; }
        public IHistoryDbAccess History { get; private init; }
        public IUploadLogDbAccess UploadLog { get; private init; }
        public ISocketProcessor Sockets { get; private init; }
        public IGameDataProvider GameData { get; private init; }
        public IUploadBehavior Behavior { get; private init; }

        public static TestResources Create()
        {
            var currentlyShownDb = new MockCurrentlyShownDbAccess();
            var historyDb = new MockHistoryDbAccess();
            var sockets = new MockSocketProcessor();
            var gameData = new MockGameDataProvider();
            var uploadLog = new MockUploadLogDbAccess();
            var logger = new LogFixture<MarketBoardUploadBehavior>();
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, uploadLog, gameData, null, logger);

            return new TestResources
            {
                CurrentlyShown = currentlyShownDb,
                History = historyDb,
                UploadLog = uploadLog,
                Sockets = sockets,
                GameData = gameData,
                Behavior = behavior,
            };
        }
    }

    [Fact]
    public void Behavior_DoesNotRun_WithoutWorldId()
    {
        var test = TestResources.Create();
        var upload = new UploadParameters
        {
            ItemId = 5333,
            Listings = new List<Listing>(),
            Sales = new List<Sale>(),
            UploaderId = "5627384655756342554",
        };

        Assert.False(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithoutItemId()
    {
        var test = TestResources.Create();
        var upload = new UploadParameters
        {
            WorldId = 74,
            Listings = new List<Listing>(),
            Sales = new List<Sale>(),
            UploaderId = "5627384655756342554",
        };

        Assert.False(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithoutListingsOrSales()
    {
        var test = TestResources.Create();
        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
        };

        Assert.False(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithZeroQuantitySales()
    {
        var test = TestResources.Create();

        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        sales[0].Quantity = 0;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Sales = sales,
        };

        Assert.False(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithZeroQuantityListings()
    {
        var test = TestResources.Create();

        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        listings[0].Quantity = 0;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Listings = listings,
        };
        Assert.False(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithInvalidStackSizeSales()
    {
        var test = TestResources.Create();

        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        sales[0].Quantity = 9999;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Sales = sales,
        };

        Assert.False(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithInvalidStackSizeListings()
    {
        var test = TestResources.Create();

        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        listings[0].Quantity = 9999;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Listings = listings,
        };
        Assert.False(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_Runs_WithoutUploaderId()
    {
        var test = TestResources.Create();
        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = new List<Listing>(),
            Sales = new List<Sale>(),
        };

        Assert.True(test.Behavior.ShouldExecute(upload));
    }

    [Fact]
    public async Task Behavior_Succeeds_ListingsAndSales()
    {
        var test = TestResources.Create();

        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(currentlyShown);
        Assert.Equal(upload.WorldId.Value, currentlyShown.WorldId);
        Assert.Equal(upload.ItemId.Value, currentlyShown.ItemId);
        Assert.NotNull(currentlyShown.Listings);
        Assert.NotEmpty(currentlyShown.Listings);

        var history = await test.History.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(history);
        Assert.Equal(upload.WorldId.Value, history.WorldId);
        Assert.Equal(upload.ItemId.Value, history.ItemId);
        Assert.NotNull(history.Sales);
        Assert.NotEmpty(history.Sales);
    }

    [Fact]
    public async Task Behavior_Succeeds_Listings()
    {
        var test = TestResources.Create();

        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(currentlyShown);
        Assert.Equal(upload.WorldId.Value, currentlyShown.WorldId);
        Assert.Equal(upload.ItemId.Value, currentlyShown.ItemId);
        Assert.NotNull(currentlyShown.Listings);
        Assert.NotEmpty(currentlyShown.Listings);

        var history = await test.History.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.Null(history);
    }

    [Fact]
    public async Task Behavior_Succeeds_Listings_WhenNone()
    {
        var test = TestResources.Create();

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = new List<Listing>(),
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(currentlyShown);
        Assert.Equal(upload.WorldId.Value, currentlyShown.WorldId);
        Assert.Equal(upload.ItemId.Value, currentlyShown.ItemId);
        Assert.NotNull(currentlyShown.Listings);
        Assert.Empty(currentlyShown.Listings);

        var history = await test.History.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.Null(history);
    }

    [Fact]
    public async Task Behavior_Succeeds_Sales()
    {
        var test = TestResources.Create();

        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };
        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var history = await test.History.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(history);
        Assert.Equal(upload.WorldId.Value, history.WorldId);
        Assert.Equal(upload.ItemId.Value, history.ItemId);
        Assert.NotNull(history.Sales);
        Assert.NotEmpty(history.Sales);
    }

    [Fact]
    public async Task Behavior_RemovesDuplicateListings()
    {
        var test = TestResources.Create();

        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        foreach (var listing in listings)
        {
            // Give all listings the same ID
            listing.ListingId = "test";
        }

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.Single(currentlyShown.Listings);
    }

    [Fact]
    public async Task Behavior_RemovesZeroValueMateria()
    {
        var test = TestResources.Create();
        var source = ApiKey.FromToken("blah", "something", true);
        var listing = new Listing
        {
            ListingId = "materia-zero",
            RetainerId = "retainer",
            RetainerName = "Retainer",
            PricePerUnit = 1000,
            Quantity = 1,
            Materia = new List<Materia>
            {
                new() { SlotId = 0, MateriaId = 0 },
                new() { SlotId = 1, MateriaId = 2 },
            },
        };
        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = new List<Listing> { listing },
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));
        Assert.Null(await test.Behavior.Execute(source, upload));

        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });
        var storedListing = Assert.Single(currentlyShown.Listings);
        var materia = Assert.Single(storedListing.Materia);
        Assert.Equal(1, materia.SlotId);
        Assert.Equal(2, materia.MateriaId);
    }

    [Fact]
    public async Task Behavior_Adds_Sale_Ids()
    {
        var test = TestResources.Create();

        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var history = await test.History.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.All(history.Sales, listing => Assert.False(listing.Id == Guid.Empty));
    }

    [Fact]
    public async Task Behavior_Adds_Sale_WorldIds()
    {
        var test = TestResources.Create();

        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var history = await test.History.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.All(history.Sales, listing => Assert.False(listing.WorldId == 0));
    }

    [Fact]
    public async Task Behavior_Logs_ListingsUploadSuccess_OnValidListings()
    {
        var test = TestResources.Create();
        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            UploaderId = "5627384655756342554",
        };

        await test.Behavior.Execute(source, upload);

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        var entry = Assert.Single(logged.Where(e => e.Event == "ListingsUploadSuccess"));
        Assert.Equal("something", entry.Application);
        Assert.Equal(74, entry.WorldId);
        Assert.Equal(5333, entry.ItemId);
        Assert.Equal(listings.Count, entry.Listings);
    }

    [Fact]
    public async Task Behavior_Logs_SalesUploadSuccess_OnValidSales()
    {
        var test = TestResources.Create();
        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        await test.Behavior.Execute(source, upload);

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        var entry = Assert.Single(logged.Where(e => e.Event == "SalesUploadSuccess"));
        Assert.Equal("something", entry.Application);
        Assert.Equal(74, entry.WorldId);
        Assert.Equal(5333, entry.ItemId);
        Assert.Equal(sales.Count, entry.Sales);
    }

    [Fact]
    public async Task Behavior_Logs_SalesUploadMalformed_OnHtmlInSales()
    {
        var test = TestResources.Create();
        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);
        sales[0] = new Sale
        {
            BuyerName = "<script>alert('pwned')</script>",
            Hq = sales[0].Hq,
            OnMannequin = sales[0].OnMannequin,
            PricePerUnit = sales[0].PricePerUnit,
            Quantity = sales[0].Quantity,
            SellerId = sales[0].SellerId,
            BuyerId = sales[0].BuyerId,
            TimestampUnixSeconds = sales[0].TimestampUnixSeconds,
        };

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        await test.Behavior.Execute(source, upload);

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        var entry = Assert.Single(logged.Where(e => e.Event == "SalesUploadMalformed"));
        Assert.Equal("something", entry.Application);
        Assert.Equal(74, entry.WorldId);
        Assert.Equal(5333, entry.ItemId);
        Assert.DoesNotContain(logged, e => e.Event == "SalesUploadSuccess");
    }

    [Fact]
    public async Task Behavior_Logs_ListingsUploadMalformed_OnHtmlInListings()
    {
        var test = TestResources.Create();
        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);
        listings[0] = new Listing
        {
            ListingId = listings[0].ListingId,
            Hq = listings[0].Hq,
            PricePerUnit = listings[0].PricePerUnit,
            Quantity = listings[0].Quantity,
            RetainerName = "<b>evil</b>",
            RetainerId = listings[0].RetainerId,
            RetainerCityId = listings[0].RetainerCityId,
            CreatorName = listings[0].CreatorName,
            OnMannequin = listings[0].OnMannequin,
            SellerId = listings[0].SellerId,
            CreatorId = listings[0].CreatorId,
            DyeId = listings[0].DyeId,
            LastReviewTimeUnixSeconds = listings[0].LastReviewTimeUnixSeconds,
            Materia = listings[0].Materia,
        };

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            UploaderId = "5627384655756342554",
        };

        await test.Behavior.Execute(source, upload);

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        var entry = Assert.Single(logged.Where(e => e.Event == "ListingsUploadMalformed"));
        Assert.Equal("something", entry.Application);
        Assert.Equal(74, entry.WorldId);
        Assert.Equal(5333, entry.ItemId);
        Assert.DoesNotContain(logged, e => e.Event == "ListingsUploadSuccess");
    }

    [Fact]
    public async Task Behavior_LogsZero_ForMissingListingsOrSales()
    {
        var test = TestResources.Create();
        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var listingsOnlyUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            UploaderId = "5627384655756342554",
        };

        await test.Behavior.Execute(source, listingsOnlyUpload);

        var listingsOnlyLogged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        Assert.NotEmpty(listingsOnlyLogged);
        Assert.All(listingsOnlyLogged, e => Assert.Equal(0, e.Sales));

        var test2 = TestResources.Create();
        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var salesOnlyUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        await test2.Behavior.Execute(source, salesOnlyUpload);

        var salesOnlyLogged = ((MockUploadLogDbAccess)test2.UploadLog).LoggedActions;
        Assert.NotEmpty(salesOnlyLogged);
        Assert.All(salesOnlyLogged, e => Assert.Equal(0, e.Listings));
    }

    [Fact]
    public async Task Behavior_Logs_UserAgent_FromUploadParameters()
    {
        var test = TestResources.Create();
        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
            UserAgent = "Universalis/1.0 Dalamud",
        };

        await test.Behavior.Execute(source, upload);

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        Assert.NotEmpty(logged);
        Assert.All(logged, e => Assert.Equal("Universalis/1.0 Dalamud", e.UserAgent));
    }

    [Fact]
    public async Task Behavior_Logs_MarketBoardUpload_OnEveryUpload()
    {
        var test = TestResources.Create();
        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        await test.Behavior.Execute(source, upload);

        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions;
        var entry = Assert.Single(logged.Where(e => e.Event == "MarketBoardUpload"));
        Assert.Equal("something", entry.Application);
        Assert.Equal(74, entry.WorldId);
        Assert.Equal(5333, entry.ItemId);
        Assert.Equal(listings.Count, entry.Listings);
        Assert.Equal(sales.Count, entry.Sales);
    }

    [Fact]
    public async Task Behavior_Adds_Sale_ItemIds()
    {
        var test = TestResources.Create();

        var stackSize = test.GameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = ApiKey.FromToken("blah", "something", true);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };

        Assert.True(test.Behavior.ShouldExecute(upload));

        var result = await test.Behavior.Execute(source, upload);
        Assert.Null(result);

        var history = await test.History.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.All(history.Sales, sale => Assert.False(sale.ItemId == 0));
    }

    private static Listing MakeListing(string listingId, string retainerId, int pricePerUnit, int quantity = 1)
    {
        return new Listing
        {
            ListingId = listingId,
            RetainerId = retainerId,
            RetainerName = "Retainer",
            PricePerUnit = pricePerUnit,
            Quantity = quantity,
        };
    }

    [Fact]
    public async Task Behavior_PreservesRetainerListings_WithUploaderRetainerId()
    {
        var test = TestResources.Create();
        var source = ApiKey.FromToken("blah", "something", true);

        // Seed listings from three retainers
        var seedUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("l1", "retA", 1000),
                MakeListing("l2", "retB", 2000),
                MakeListing("l3", "retC", 3000),
            },
        };

        Assert.True(test.Behavior.ShouldExecute(seedUpload));
        Assert.Null(await test.Behavior.Execute(source, seedUpload));

        // Second upload omits retA, but requests preservation via UploaderRetainerId
        var secondUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            UploaderRetainerId = "retA",
            Listings = new List<Listing>
            {
                MakeListing("l2", "retB", 2000),
                MakeListing("l3", "retC", 3000),
            },
        };

        Assert.True(test.Behavior.ShouldExecute(secondUpload));
        Assert.Null(await test.Behavior.Execute(source, secondUpload));

        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = 74,
            ItemId = 5333,
        });

        Assert.NotNull(currentlyShown);
        var listingIds = currentlyShown.Listings.Select(l => l.ListingId).ToList();
        Assert.Contains("l1", listingIds); // preserved from retA
        Assert.Contains("l2", listingIds);
        Assert.Contains("l3", listingIds);
    }

    [Fact]
    public async Task Behavior_RemovesNonUploadedListings_WithoutUploaderRetainerId()
    {
        var test = TestResources.Create();
        var source = ApiKey.FromToken("blah", "something", true);

        var seedUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("l1", "retA", 1000),
                MakeListing("l2", "retB", 2000),
                MakeListing("l3", "retC", 3000),
            },
        };

        await test.Behavior.Execute(source, seedUpload);

        // Second upload omits retA, no preservation requested
        var secondUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("l2", "retB", 2000),
                MakeListing("l3", "retC", 3000),
            },
        };

        await test.Behavior.Execute(source, secondUpload);

        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = 74,
            ItemId = 5333,
        });

        Assert.NotNull(currentlyShown);
        var listingIds = currentlyShown.Listings.Select(l => l.ListingId).ToList();
        Assert.DoesNotContain("l1", listingIds); // removed by full-replace
        Assert.Contains("l2", listingIds);
        Assert.Contains("l3", listingIds);
    }

    [Fact]
    public async Task Behavior_RejectsUpload_WhenRetainerListingAlsoUploaded()
    {
        var test = TestResources.Create();
        var source = ApiKey.FromToken("blah", "something", true);

        var seedUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            Listings = new List<Listing>
            {
                MakeListing("l1", "retA", 1000),
            },
        };

        await test.Behavior.Execute(source, seedUpload);

        var loggedBeforeReject = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions.Count;

        // Upload retA's listing again while requesting preservation — contract violation
        var secondUpload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "uploader1",
            UploaderRetainerId = "retA",
            Listings = new List<Listing>
            {
                MakeListing("l1", "retA", 1000),
            },
        };

        var result = await test.Behavior.Execute(source, secondUpload);

        Assert.NotNull(result);
        Assert.IsType<BadRequestResult>(result);

        // Only check logs from the rejected upload
        var logged = ((MockUploadLogDbAccess)test.UploadLog).LoggedActions
            .Skip(loggedBeforeReject)
            .ToList();
        Assert.Contains(logged, e => e.Event == "ListingsUploadMalformed");
        Assert.DoesNotContain(logged, e => e.Event == "ListingsUploadSuccess");

        // Existing data untouched
        var currentlyShown = await test.CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = 74,
            ItemId = 5333,
        });

        Assert.NotNull(currentlyShown);
        Assert.Single(currentlyShown.Listings);
    }
}
