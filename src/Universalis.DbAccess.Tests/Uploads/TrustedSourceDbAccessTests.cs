using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads
{
    public class TrustedSourceDbAccessTests
    {
        public TrustedSourceDbAccessTests()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            client.DropDatabase(Constants.DatabaseName);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new TrustedSourceDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeTrustedSource();
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new TrustedSourceDbAccess(Constants.DatabaseName);
            var output = await db.Retrieve(new TrustedSourceQuery { ApiKeySha256 = "babaef32" });
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new TrustedSourceDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeTrustedSource();
            await db.Update(document, new TrustedSourceQuery { ApiKeySha256 = document.ApiKeySha256 });
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new TrustedSourceDbAccess(Constants.DatabaseName);
            await db.Delete(new TrustedSourceQuery { ApiKeySha256 = "babaef32" });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new TrustedSourceDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeTrustedSource();
            await db.Create(document);

            var output = await db.Retrieve(new TrustedSourceQuery { ApiKeySha256 = document.ApiKeySha256 });
            Assert.NotNull(output);
            Assert.Equal(document.ApiKeySha256, output.ApiKeySha256);
            Assert.Equal(document.Name, output.Name);
            Assert.Equal(document.UploadCount, output.UploadCount);
        }

        [Fact]
        public async Task Increment_DoesNotCreateIfNone()
        {
            var db = new TrustedSourceDbAccess(Constants.DatabaseName);
            await db.Increment(new TrustedSourceQuery { ApiKeySha256 = "bbbbbbbb" });
            var output = await db.GetUploaderCounts();
            Assert.NotNull(output);
            Assert.Empty(output);
        }

        [Fact]
        public async Task Increment_DoesPersist()
        {
            var db = new TrustedSourceDbAccess(Constants.DatabaseName);
            var document = SeedDataGenerator.MakeTrustedSource();
            await db.Create(document);
            await db.Increment(new TrustedSourceQuery { ApiKeySha256 = document.ApiKeySha256 });
            var output = (await db.GetUploaderCounts())?.ToList();
            Assert.NotNull(output);
            Assert.Single(output);
            Assert.Equal(document.Name, output[0].Name);
            Assert.Equal(document.UploadCount + 1, output[0].UploadCount);
        }
    }
}