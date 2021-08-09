using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities;
using Xunit;

namespace Universalis.DbAccess.Tests
{
    public class ContentDbAccessTests : IDisposable
    {
        private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(ContentDbAccessTests));

        private readonly IMongoClient _client;
        private readonly IConnectionThrottlingPipeline _throttler;

        public ContentDbAccessTests()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            _client.DropDatabase(Database);

            _throttler = new ConnectionThrottlingPipeline(_client);
        }

        public void Dispose()
        {
            _client.DropDatabase(Database);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new ContentDbAccess(_client, _throttler, Database);

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
            var db = new ContentDbAccess(_client, _throttler, Database);
            var output = await db.Retrieve(new ContentQuery { ContentId = "a" });
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new ContentDbAccess(_client, _throttler, Database);

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
            var db = new ContentDbAccess(_client, _throttler, Database);
            await db.Delete(new ContentQuery { ContentId = "a" });
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new ContentDbAccess(_client, _throttler, Database);

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
