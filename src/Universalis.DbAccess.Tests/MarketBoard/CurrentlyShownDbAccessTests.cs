using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.MarketBoard
{
    public class CurrentlyShownDbAccessTests : IDisposable
    {
        private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(CurrentlyShownDbAccessTests));

        private readonly IMongoClient _client;
        
        public CurrentlyShownDbAccessTests()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            _client.DropDatabase(Database);
        }

        public void Dispose()
        {
            _client.DropDatabase(Database);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(_client, Database);
            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(_client, Database);
            var output = await db.Retrieve(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
            Assert.Null(output);
        }

        [Fact]
        public async Task RetrieveMany_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(_client, Database);
            var output = await db.RetrieveMany(new CurrentlyShownManyQuery { WorldIds = new uint[] { 74 }, ItemId = 5333 });
            Assert.NotNull(output);
            Assert.Empty(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(_client, Database);
            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            var query = new CurrentlyShownQuery { WorldId = document.WorldId, ItemId = document.ItemId };

            await db.Update(document, query);
            await db.Update(document, query);

            document.LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            await db.Update(document, query);

            var retrieved = await db.Retrieve(query);
            Assert.Equal(document.LastUploadTimeUnixMilliseconds, retrieved.LastUploadTimeUnixMilliseconds);
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new CurrentlyShownDbAccess(_client, Database);
            await db.Delete(new CurrentlyShownQuery { WorldId = 74, ItemId = 5333 });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new CurrentlyShownDbAccess(_client, Database);

            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            await db.Create(document);

            var output = await db.Retrieve(new CurrentlyShownQuery { WorldId = document.WorldId, ItemId = document.ItemId });
            Assert.NotNull(output);
        }

        [Fact]
        public async Task RetrieveMany_ReturnsData()
        {
            var db = new CurrentlyShownDbAccess(_client, Database);

            var document = SeedDataGenerator.MakeCurrentlyShown(74, 5333);
            await db.Create(document);

            var output = (await db.RetrieveMany(new CurrentlyShownManyQuery { WorldIds = new[] { document.WorldId }, ItemId = document.ItemId }))?.ToList();
            Assert.NotNull(output);
            Assert.Single(output);
            Assert.Equal(document.WorldId, output[0].WorldId);
            Assert.Equal(document.ItemId, output[0].ItemId);
            Assert.Equal(document.LastUploadTimeUnixMilliseconds, output[0].LastUploadTimeUnixMilliseconds);
            Assert.Equal(document.Listings, output[0].Listings);
            Assert.Equal(document.RecentHistory, output[0].RecentHistory);
            Assert.Equal(document.UploaderIdHash, output[0].UploaderIdHash);
        }
    }
}