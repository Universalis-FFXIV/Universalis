using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads
{
    public class RecentlyUpdatedItemsDbAccessTests : IDisposable
    {
        private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(RecentlyUpdatedItemsDbAccessTests));

        private readonly MongoClient _client;

        public RecentlyUpdatedItemsDbAccessTests()
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
        public async Task Retrieve_DoesNotThrow()
        {
            IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(Database);
            var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
            Assert.Null(output);
        }

        [Fact]
        public async Task Push_DoesNotThrow()
        {
            IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(Database);
            await db.Push(5333);
        }

        [Fact]
        public async Task Push_DoesRetrieve()
        {
            IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(Database);
            await db.Push(5333);
            var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
            Assert.NotNull(output);
            Assert.Single(output.Items);
            Assert.Equal(5333U, output.Items[0]);
        }

        [Fact]
        public async Task PushTwice_DoesRetrieve()
        {
            IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(Database);
            await db.Push(5333);
            await db.Push(5);
            var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
            Assert.NotNull(output);
            Assert.Equal(5U, output.Items[0]);
            Assert.Equal(5333U, output.Items[1]);
        }

        [Fact]
        public async Task PushSameTwice_DoesReorder()
        {
            IRecentlyUpdatedItemsDbAccess db = new RecentlyUpdatedItemsDbAccess(Database);
            await db.Push(5333);
            await db.Push(5);
            await db.Push(5333);
            var output = await db.Retrieve(new RecentlyUpdatedItemsQuery());
            Assert.NotNull(output);
            Assert.Equal(5333U, output.Items[0]);
            Assert.Equal(5U, output.Items[1]);
        }
    }
}