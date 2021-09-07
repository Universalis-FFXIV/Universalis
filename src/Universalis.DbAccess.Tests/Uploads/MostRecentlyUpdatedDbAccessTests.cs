using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads
{
    public class MostRecentlyUpdatedDbAccessTests : IDisposable
    {
        private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(MostRecentlyUpdatedDbAccessTests));

        private readonly IMongoClient _client;

        public MostRecentlyUpdatedDbAccessTests()
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
        public void Constructor_CanBeCalledMultipleTimes()
        {
            for (var i = 0; i < 10; i++)
            {
                _ = new MostRecentlyUpdatedDbAccess(_client, Database);
            }
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            var db = new MostRecentlyUpdatedDbAccess(_client, Database);
            await db.Create(new MostRecentlyUpdated
            {
                WorldId = 74,
                Uploads = new List<WorldItemUpload>
                {
                    new()
                    {
                        ItemId = 5333,
                        WorldId = 74,
                        LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    },
                },
            });
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new MostRecentlyUpdatedDbAccess(_client, Database);
            var output = await db.Retrieve(new MostRecentlyUpdatedQuery());
            Assert.Null(output);
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new MostRecentlyUpdatedDbAccess(_client, Database);
            await db.Create(new MostRecentlyUpdated
            {
                WorldId = 74,
                Uploads = new List<WorldItemUpload>
                {
                    new()
                    {
                        ItemId = 5333,
                        WorldId = 74,
                        LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    },
                },
            });

            var output = await db.Retrieve(new MostRecentlyUpdatedQuery { WorldId = 74 });
            Assert.NotNull(output);
        }

        [Fact]
        public async Task Operations_RespectDocumentCap()
        {
            var db = new MostRecentlyUpdatedDbAccess(_client, Database);
            for (var i = 0; i < MostRecentlyUpdatedDbAccess.MaxItems * 2; i++)
            {
                await db.Push(74, new WorldItemUpload
                {
                    ItemId = 5333 + (uint)i,
                    WorldId = 74,
                    LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                });
            }

            var output = await db.RetrieveMany(new MostRecentlyUpdatedManyQuery { WorldIds = new[] { 74U } });
            Assert.Single(output);
            Assert.Equal(MostRecentlyUpdatedDbAccess.MaxItems, output[0].Uploads.Count);
        }
    }
}