using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads
{
    public class UploadCountHistoryDbAccessTests : IDisposable
    {
        private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(UploadCountHistoryDbAccessTests));

        private readonly IMongoClient _client;
        private readonly IConnectionThrottlingPipeline _throttler;

        public UploadCountHistoryDbAccessTests()
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
            var db = new UploadCountHistoryDbAccess(_client, _throttler, Database);
            await db.Create(new UploadCountHistory
            {
                LastPush = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                UploadCountByDay = new List<double> { 1 },
            });
        }

        [Fact]
        public async Task Retrieve_DoesNotThrow()
        {
            var db = new UploadCountHistoryDbAccess(_client, _throttler, Database);
            var output = await db.Retrieve(new UploadCountHistoryQuery());
            Assert.Null(output);
        }

        [Fact]
        public async Task Update_DoesNotThrow()
        {
            var db = new UploadCountHistoryDbAccess(_client, _throttler, Database);
            await db.Update(new UploadCountHistory
            {
                LastPush = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                UploadCountByDay = new List<double> { 1 },
            }, new UploadCountHistoryQuery());
        }

        [Fact]
        public async Task Create_DoesInsert()
        {
            var db = new UploadCountHistoryDbAccess(_client, _throttler, Database);
            var document = new UploadCountHistory
            {
                LastPush = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                UploadCountByDay = new List<double> { 1 },
            };
            await db.Create(document);

            var output = await db.Retrieve(new UploadCountHistoryQuery());
            Assert.NotNull(output);
            Assert.Equal(document.LastPush, output.LastPush);
            Assert.Equal(document.UploadCountByDay, output.UploadCountByDay);
        }
    }
}