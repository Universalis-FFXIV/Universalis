using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.AccessControl;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class TrustedSourceDbAccessTests
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

    private class MockApiKeyStore : IApiKeyStore
    {
        private readonly Dictionary<string, ApiKey> _data = new();

        public Task Insert(ApiKey apiKey, CancellationToken cancellationToken = default)
        {
            _data[apiKey.TokenSha512] = apiKey;
            return Task.CompletedTask;
        }

        public Task<ApiKey> Retrieve(string tokenSha512, CancellationToken cancellationToken = default)
        {
            return !_data.ContainsKey(tokenSha512)
                ? Task.FromResult<ApiKey>(null)
                : Task.FromResult(_data[tokenSha512]);
        }
    }

    [Fact]
    public async Task Create_DoesNotThrow()
    {
        var db = new TrustedSourceDbAccess(new MockApiKeyStore(), new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeApiKey();
        await db.Create(document);
    }

    [Fact]
    public async Task Retrieve_DoesNotThrow()
    {
        var db = new TrustedSourceDbAccess(new MockApiKeyStore(), new MockSourceUploadCountStore());
        var output = await db.Retrieve(new TrustedSourceQuery { ApiKeySha512 = "babaef32" });
        Assert.Null(output);
    }

    [Fact]
    public async Task Create_DoesInsert()
    {
        var db = new TrustedSourceDbAccess(new MockApiKeyStore(), new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeApiKey();
        await db.Create(document);

        var output = await db.Retrieve(new TrustedSourceQuery { ApiKeySha512 = document.TokenSha512 });
        Assert.NotNull(output);
        Assert.Equal(document.TokenSha512, output.TokenSha512);
        Assert.Equal(document.Name, output.Name);
        Assert.Equal(document.CanUpload, output.CanUpload);
    }

    [Fact]
    public async Task Increment_DoesNotThrow()
    {
        var db = new TrustedSourceDbAccess(new MockApiKeyStore(), new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeApiKey();

        await db.Create(document);
        await db.Increment(new TrustedSourceQuery { ApiKeySha512 = document.TokenSha512 });
    }

    [Fact]
    public async Task Increment_DoesPersist()
    {
        var db = new TrustedSourceDbAccess(new MockApiKeyStore(), new MockSourceUploadCountStore());
        var document = SeedDataGenerator.MakeApiKey();
        await db.Create(document);
        await db.Increment(new TrustedSourceQuery { ApiKeySha512 = document.TokenSha512 });
        var output = (await db.GetUploaderCounts())?.ToList();
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal(document.Name, output[0].Name);
        Assert.Equal(1, output[0].UploadCount);
    }
}