using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class WorldUploadCountDbAccessTests : IDisposable
{
    private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(WorldUploadCountDbAccessTests));

    private readonly IMongoClient _client;
        
    public WorldUploadCountDbAccessTests()
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
    public async Task GetWorldUploadCounts_DoesNotThrow()
    {
        IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(_client, Database);
        var output = await db.GetWorldUploadCounts();
        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task Increment_DoesNotThrow()
    {
        IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(_client, Database);
        var query = new WorldUploadCountQuery { WorldName = "Coeurl" };

        await db.Increment(query);
        await db.Increment(query);
    }

    [Fact]
    public async Task Increment_DoesRetrieve()
    {
        IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(_client, Database);
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