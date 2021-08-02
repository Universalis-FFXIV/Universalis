using MongoDB.Driver;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads
{
    public class FlaggedUploaderDbAccessTests
    {
        public FlaggedUploaderDbAccessTests()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            client.DropDatabase(Constants.DatabaseName);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new FlaggedUploaderDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeFlaggedUploader();
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new FlaggedUploaderDbAccess(Constants.DatabaseName);
            var output = await db.Retrieve(new FlaggedUploaderQuery { UploaderIdHash = "affffe" });
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new FlaggedUploaderDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeFlaggedUploader();
            await db.Update(document, new FlaggedUploaderQuery { UploaderIdHash = document.UploaderIdHash });
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new FlaggedUploaderDbAccess(Constants.DatabaseName);
            await db.Delete(new FlaggedUploaderQuery { UploaderIdHash = "affffe" });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new FlaggedUploaderDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeFlaggedUploader();
            await db.Create(document);

            var output = await db.Retrieve(new FlaggedUploaderQuery { UploaderIdHash = document.UploaderIdHash });
            Assert.NotNull(output);
        }
    }
}