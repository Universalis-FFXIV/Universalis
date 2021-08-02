using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads
{
    public class FlaggedUploaderDbAccessTests : IDisposable
    {
        private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(FlaggedUploaderDbAccessTests));

        private readonly MongoClient _client;

        public FlaggedUploaderDbAccessTests()
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
            var db = new FlaggedUploaderDbAccess(Database);
            var document = SeedDataGenerator.MakeFlaggedUploader();
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new FlaggedUploaderDbAccess(Database);
            var output = await db.Retrieve(new FlaggedUploaderQuery { UploaderIdHash = "affffe" });
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new FlaggedUploaderDbAccess(Database);
            var document = SeedDataGenerator.MakeFlaggedUploader();
            await db.Update(document, new FlaggedUploaderQuery { UploaderIdHash = document.UploaderIdHash });
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new FlaggedUploaderDbAccess(Database);
            await db.Delete(new FlaggedUploaderQuery { UploaderIdHash = "affffe" });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new FlaggedUploaderDbAccess(Database);
            var document = SeedDataGenerator.MakeFlaggedUploader();
            await db.Create(document);

            var output = await db.Retrieve(new FlaggedUploaderQuery { UploaderIdHash = document.UploaderIdHash });
            Assert.NotNull(output);
        }
    }
}