using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.Uploads;
using Xunit;
using Listing = Universalis.Application.Uploads.Schema.Listing;

namespace Universalis.Application.Tests.Uploads.Behaviors
{
    public class MarketBoardUploadBehaviorTests
    {
        [Fact]
        public void Behavior_DoesNotRun_WithoutWorldId()
        {
            var currentlyShownDb = new MockCurrentlyShownDbAccess();
            var historyDb = new MockHistoryDbAccess();
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb);

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
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb);

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
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb);

            var upload = new UploadParameters
            {
                WorldId = 74,
                ItemId = 5333,
                UploaderId = "5627384655756342554",
            };
            Assert.False(behavior.ShouldExecute(upload));
        }

        [Fact]
        public void Behavior_DoesNotRun_WithoutUploaderId()
        {
            var currentlyShownDb = new MockCurrentlyShownDbAccess();
            var historyDb = new MockHistoryDbAccess();
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb);

            var source = new TrustedSource
            {
                ApiKeySha256 = "2f44abe6",
                Name = "test runner",
                UploadCount = 0,
            };

            var upload = new UploadParameters
            {
                WorldId = 74,
                ItemId = 5333,
                Listings = new List<Listing>(),
                Sales = new List<Sale>(),
            };
            Assert.False(behavior.ShouldExecute(upload));
        }

        [Fact]
        public async Task Behavior_Succeeds_ListingsAndSales()
        {
            var currentlyShownDb = new MockCurrentlyShownDbAccess();
            var historyDb = new MockHistoryDbAccess();
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb);

            var (listings, sales) = SeedDataGenerator.GetUploadListingsAndSales(74, 5333);

            var source = new TrustedSource
            {
                ApiKeySha256 = "2f44abe6",
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
            Assert.Equal(upload.UploaderId, currentlyShown.UploaderIdHash);
            Assert.NotNull(currentlyShown.Listings);
            Assert.NotEmpty(currentlyShown.Listings);
            Assert.NotNull(currentlyShown.RecentHistory);
            Assert.NotEmpty(currentlyShown.RecentHistory);

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
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb);

            var (listings, _) = SeedDataGenerator.GetUploadListingsAndSales(74, 5333);

            var source = new TrustedSource
            {
                ApiKeySha256 = "2f44abe6",
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
            Assert.Equal(upload.UploaderId, currentlyShown.UploaderIdHash);
            Assert.NotNull(currentlyShown.Listings);
            Assert.NotEmpty(currentlyShown.Listings);
            Assert.NotNull(currentlyShown.RecentHistory);
            Assert.Empty(currentlyShown.RecentHistory);

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
            var behavior = new MarketBoardUploadBehavior(currentlyShownDb, historyDb);

            var (_, sales) = SeedDataGenerator.GetUploadListingsAndSales(74, 5333);

            var source = new TrustedSource
            {
                ApiKeySha256 = "2f44abe6",
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
            Assert.Equal(upload.UploaderId, currentlyShown.UploaderIdHash);
            Assert.NotNull(currentlyShown.Listings);
            Assert.Empty(currentlyShown.Listings);
            Assert.NotNull(currentlyShown.RecentHistory);
            Assert.NotEmpty(currentlyShown.RecentHistory);

            var history = await historyDb.Retrieve(new HistoryQuery
            {
                WorldId = upload.WorldId.Value,
                ItemId = upload.ItemId.Value,
            });

            Assert.NotNull(history);
            Assert.NotNull(history.Sales);
            Assert.NotEmpty(history.Sales);
        }
    }
}