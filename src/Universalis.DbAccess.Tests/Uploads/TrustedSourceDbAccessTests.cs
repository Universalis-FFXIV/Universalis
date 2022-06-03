using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class TrustedSourceDbAccessTests : IDisposable
{
    private class MockSourceUploadCountStore : ISourceUploadCountStore
    {
        private readonly Dictionary<string, long> _counts = new();

        public Task IncrementCounter(string key, string counterName)
        {
            if (!_counts.ContainsKey(counterName))
            {
                _counts[counterName] = 0;
            }

            _counts[counterName]++;
            
            return Task.CompletedTask;
        }

        public Task<IList<KeyValuePair<string, long>>> GetCounterValues(string key)
        {
            return Task.FromResult((IList<KeyValuePair<string, long>>)_counts.Select(c => c).ToList());
        }
    }
    
    private static readonly string Database = CollectionUtils.GetDatabaseName(nameof(TrustedSourceDbAccessTests));

    private readonly IMongoClient _client;
    private readonly IConnectionMultiplexer _redis;
        
    public TrustedSourceDbAccessTests()
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
        var db = new TrustedSourceDbAccess(_client, Database, new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeTrustedSource();
        await db.Create(document);
    }

    [Fact]
    public async Task Retrieve_DoesNotThrow()
    {
        var db = new TrustedSourceDbAccess(_client, Database, new MockSourceUploadCountStore());
        var output = await db.Retrieve(new TrustedSourceQuery { ApiKeySha512 = "babaef32" });
        Assert.Null(output);
    }

    [Fact]
    public async Task Update_DoesNotThrow()
    {
        var db = new TrustedSourceDbAccess(_client, Database, new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeTrustedSource();
        var query = new TrustedSourceQuery { ApiKeySha512 = document.ApiKeySha512 };

        await db.Update(document, query);
        await db.Update(document, query);

        document.UploadCount = 74;
        await db.Update(document, query);
    }

    [Fact]
    public async Task Delete_DoesNotThrow()
    {
        var db = new TrustedSourceDbAccess(_client, Database, new MockSourceUploadCountStore());
        await db.Delete(new TrustedSourceQuery { ApiKeySha512 = "babaef32" });
    }

    [Fact]
    public async Task Create_DoesInsert()
    {
        var db = new TrustedSourceDbAccess(_client, Database, new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeTrustedSource();
        await db.Create(document);

        var output = await db.Retrieve(new TrustedSourceQuery { ApiKeySha512 = document.ApiKeySha512 });
        Assert.NotNull(output);
        Assert.Equal(document.ApiKeySha512, output.ApiKeySha512);
        Assert.Equal(document.Name, output.Name);
        Assert.Equal(document.UploadCount, output.UploadCount);
    }

    [Fact]
    public async Task Increment_DoesNotThrow()
    {
        var db = new TrustedSourceDbAccess(_client, Database, new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeTrustedSource();

        await db.Create(document);
        await db.Increment(document.Name);
    }

    [Fact]
    public async Task Increment_DoesPersist()
    {
        var db = new TrustedSourceDbAccess(_client, Database, new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeTrustedSource();
        await db.Create(document);
        await db.Increment(document.Name);
        var output = (await db.GetUploaderCounts())?.ToList();
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(document.Name, output[0].Name);
        Assert.Equal(document.UploadCount + 1, output[0].UploadCount);
    }
}