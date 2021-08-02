using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard
{
    public class CurrentlyShownDbAccessTests
    {
        public CurrentlyShownDbAccessTests()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            client.DropDatabase(Constants.DatabaseName);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(Constants.DatabaseName);
            var output = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
            Assert.Null(output);
        }

        [Fact]
        public async Task RetrieveMany_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(Constants.DatabaseName);
            var output = await db.RetrieveMany(new CurrentlyShownManyQuery { WorldIds = new uint[] { 74 }, ItemId = 5333 });
            Assert.NotNull(output);
            Assert.Empty(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            await db.Update(document, new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(Constants.DatabaseName);
            await db.Delete(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new CurrentlyShownDbAccess(Constants.DatabaseName);

            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            await db.Create(document);

            var output = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
            Assert.NotNull(output);
        }

        [Fact]
        public async Task RetrieveMany_ReturnsData()
        {
            var db = new CurrentlyShownDbAccess(Constants.DatabaseName);

            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            await db.Create(document);

            var output = (await db.RetrieveMany(new CurrentlyShownManyQuery { WorldIds = new uint[] { 74 }, ItemId = 5333 }))?.ToList();
            Assert.NotNull(output);
            Assert.Single(output);
            Assert.Equal(output[0].WorldId, document.WorldId);
            Assert.Equal(output[0].ItemId, document.ItemId);
            Assert.Equal(output[0].LastUploadTimeUnixMilliseconds, document.LastUploadTimeUnixMilliseconds);
            Assert.Equal(output[0].Listings, document.Listings);
            Assert.Equal(output[0].RecentHistory, document.RecentHistory);
            Assert.Equal(output[0].UploaderIdHash, document.UploaderIdHash);
        }
    }
}