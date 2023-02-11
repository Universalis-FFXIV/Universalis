using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Realtime;
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
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, uploadLog, gameData, null);

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
}