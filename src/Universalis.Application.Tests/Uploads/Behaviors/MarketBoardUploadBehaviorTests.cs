using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Caching;
using Universalis.Application.Controllers;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Tests.Mocks.Realtime;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.Uploads;
using Xunit;
using Listing = Universalis.Application.Uploads.Schema.Listing;

namespace Universalis.Application.Tests.Uploads.Behaviors;

public class MarketBoardUploadBehaviorTests
{
    [Fact]
    public void Behavior_DoesNotRun_WithoutWorldId()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);

        var upload = new UploadParameters
        {
            ItemId = 5333,
            Listings = new List<Listing>(),
            Sales = new List<Sale>(),
            UploaderId = "5627384655756342554",
        };
        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithoutItemId()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);

        var upload = new UploadParameters
        {
            WorldId = 74,
            Listings = new List<Listing>(),
            Sales = new List<Sale>(),
            UploaderId = "5627384655756342554",
        };
        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_DoesNotRun_WithoutListingsOrSales()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
        };
        Assert.False(behavior.ShouldExecute(upload));
    }
    
    [Fact]
    public void Behavior_DoesNotRun_WithZeroQuantitySales()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);
        
        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        sales[0].Quantity = 0;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Sales = sales,
        };
        Assert.False(behavior.ShouldExecute(upload));
    }
    
    [Fact]
    public void Behavior_DoesNotRun_WithZeroQuantityListings()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);
        
        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        listings[0].Quantity = 0;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Listings = listings,
        };
        Assert.False(behavior.ShouldExecute(upload));
    }
    
    [Fact]
    public void Behavior_DoesNotRun_WithInvalidStackSizeSales()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);
        
        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        sales[0].Quantity = 9999;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Sales = sales,
        };
        Assert.False(behavior.ShouldExecute(upload));
    }
    
    [Fact]
    public void Behavior_DoesNotRun_WithInvalidStackSizeListings()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);
        
        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333);
        listings[0].Quantity = 9999;

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            UploaderId = "5627384655756342554",
            Listings = listings,
        };
        Assert.False(behavior.ShouldExecute(upload));
    }

    [Fact]
    public void Behavior_Runs_WithoutUploaderId()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = new List<Listing>(),
            Sales = new List<Sale>(),
        };
        Assert.True(behavior.ShouldExecute(upload));
    }

    [Fact]
    public async Task Behavior_Succeeds_ListingsAndSales()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);

        var stackSize = gameData.MarketableItemStackSizes()[5333];
        var (listings, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = new TrustedSource
        {
            ApiKeySha512 = "2f44abe6",
            Name = "test runner",
            UploadCount = 0,
        };

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };
        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(source, upload);
        Assert.Null(result);

        var currentlyShown = await currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(currentlyShown);
        Assert.Equal(upload.WorldId.Value, currentlyShown.WorldId);
        Assert.Equal(upload.ItemId.Value, currentlyShown.ItemId);
        Assert.NotNull(currentlyShown.Listings);
        Assert.NotEmpty(currentlyShown.Listings);

        var history = await historyDb.Retrieve(new HistoryQuery
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
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);

        var stackSize = gameData.MarketableItemStackSizes()[5333];
        var (listings, _) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = new TrustedSource
        {
            ApiKeySha512 = "2f44abe6",
            Name = "test runner",
            UploadCount = 0,
        };

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Listings = listings,
            UploaderId = "5627384655756342554",
        };
        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(source, upload);
        Assert.Null(result);

        var currentlyShown = await currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(currentlyShown);
        Assert.Equal(upload.WorldId.Value, currentlyShown.WorldId);
        Assert.Equal(upload.ItemId.Value, currentlyShown.ItemId);
        Assert.NotNull(currentlyShown.Listings);
        Assert.NotEmpty(currentlyShown.Listings);

        var history = await historyDb.Retrieve(new HistoryQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.Null(history);
    }

    [Fact]
    public async Task Behavior_Succeeds_Sales()
    {
        var currentlyShownDb = new MockCurrentlyShownDbAccess();
        var historyDb = new MockHistoryDbAccess();
        var cache = new MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>(1);
        var sockets = new MockSocketProcessor();
        var gameData = new MockGameDataProvider();
        var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb, cache, sockets, gameData);

        var stackSize = gameData.MarketableItemStackSizes()[5333];
        var (_, sales) = SchemaSeedDataGenerator.GetUploadListingsAndSales(74, 5333, stackSize);

        var source = new TrustedSource
        {
            ApiKeySha512 = "2f44abe6",
            Name = "test runner",
            UploadCount = 0,
        };

        var upload = new UploadParameters
        {
            WorldId = 74,
            ItemId = 5333,
            Sales = sales,
            UploaderId = "5627384655756342554",
        };
        Assert.True(behavior.ShouldExecute(upload));

        var result = await behavior.Execute(source, upload);
        Assert.Null(result);

        var currentlyShown = await currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = upload.WorldId.Value,
            ItemId = upload.ItemId.Value,
        });

        Assert.NotNull(currentlyShown);
        Assert.Equal(upload.WorldId.Value, currentlyShown.WorldId);
        Assert.Equal(upload.ItemId.Value, currentlyShown.ItemId);
        Assert.NotNull(currentlyShown.Listings);
        Assert.Empty(currentlyShown.Listings);

        var history = await historyDb.Retrieve(new HistoryQuery
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
}