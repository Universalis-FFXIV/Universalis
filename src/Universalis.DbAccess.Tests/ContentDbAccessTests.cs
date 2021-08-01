using MongoDB.Driver;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities;
using Xunit;

namespace Universalis.DbAccess.Tests
{
    public class ContentDbAccessTests
    {
        public ContentDbAccessTests()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            client.DropDatabase(Constants.DatabaseName);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new ContentDbAccess(Constants.DatabaseName);

            var document = new Content
            {
                ContentId = "a",
                ContentType = ContentKind.Player,
                CharacterName = "b",
            };
            await db.Create(document);
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new ContentDbAccess(Constants.DatabaseName);
            var output = await db.Retrieve(new ContentQuery { ContentId = "a" });
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new ContentDbAccess(Constants.DatabaseName);

            var document = new Content
            {
                ContentId = "a",
                ContentType = ContentKind.Player,
                CharacterName = "b",
            };
            await db.Update(document, new ContentQuery { ContentId = document.ContentId });
        }

        [Fact]
        public async Task Delete_DoesNotThrow()
        {
            var db = new ContentDbAccess(Constants.DatabaseName);
            await db.Delete(new ContentQuery { ContentId = "a" });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new ContentDbAccess(Constants.DatabaseName);

            var document = new Content
            {
                ContentId = "a",
                ContentType = ContentKind.Player,
                CharacterName = "b",
            };
            await db.Create(document);

            var output = await db.Retrieve(new ContentQuery { ContentId = document.ContentId });
            Assert.NotNull(output);
            Assert.Equal(document.ContentId, output.ContentId);
            Assert.Equal(document.ContentType, output.ContentType);
            Assert.Equal(document.CharacterName, output.CharacterName);
        }
    }
}
