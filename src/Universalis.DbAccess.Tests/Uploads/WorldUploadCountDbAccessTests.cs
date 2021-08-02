using MongoDB.Driver;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads
{
    public class WorldUploadCountDbAccessTests
    {
        public WorldUploadCountDbAccessTests()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            client.DropDatabase(Constants.DatabaseName);
        }

        [Fact]
        public async Task GetWorldUploadCounts_DoesNotThrow()
        {
            IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(Constants.DatabaseName);
            var output = await db.GetWorldUploadCounts();
            Assert.NotNull(output);
            Assert.Empty(output);
        }

        [Fact]
        public async Task Increment_DoesNotThrow()
        {
            IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(Constants.DatabaseName);
            await db.Increment(new WorldUploadCountQuery { WorldName = "Coeurl" });
        }

        [Fact]
        public async Task Increment_DoesRetrieve()
        {
            IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(Constants.DatabaseName);
            await db.Increment(new WorldUploadCountQuery { WorldName = "Coeurl" });
            var output = (await db.GetWorldUploadCounts()).ToList();
            Assert.NotNull(output);
            Assert.Single(output);
            Assert.Equal(new WorldUploadCount
            {
                WorldName = "Coeurl",
                Count = 1,
            }, output[0]);
        }
    }
}