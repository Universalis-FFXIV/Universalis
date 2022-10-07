using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Xunit;

namespace Universalis.DbAccess.Tests.Uploads;

public class WorldUploadCountDbAccessTests
{
    private class MockWorldUploadCountStore : IWorldUploadCountStore
    {
        private readonly Dictionary<string, long> _counts = new();

        public Task Increment(string key, string worldName, CancellationToken cancellationToken = default)
        {
            if (!_counts.ContainsKey(worldName))
            {
                _counts[worldName] = 0;
            }
            
            _counts[worldName]++;
            return Task.CompletedTask;
        }

        public Task<IList<KeyValuePair<string, long>>> GetWorldUploadCounts(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IList<KeyValuePair<string, long>>)_counts.ToList());
        }
    }

    [Fact]
    public async Task GetWorldUploadCounts_DoesNotThrow()
    {
        IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(new MockWorldUploadCountStore());
        var output = await db.GetWorldUploadCounts();
        Assert.NotNull(output);
        Assert.Empty(output);
    }

    [Fact]
    public async Task Increment_DoesNotThrow()
    {
        IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(new MockWorldUploadCountStore());
        var query = new WorldUploadCountQuery { WorldName = "Coeurl" };

        await db.Increment(query);
        await db.Increment(query);
    }

    [Fact]
    public async Task Increment_DoesRetrieve()
    {
        IWorldUploadCountDbAccess db = new WorldUploadCountDbAccess(new MockWorldUploadCountStore());
        await db.Increment(new WorldUploadCountQuery { WorldName = "Coeurl" });
        var output = (await db.GetWorldUploadCounts()).ToList();
        Assert.NotNull(output);
        Assert.Single(output);
        Assert.Equal("Coeurl", output[0].WorldName);
        Assert.Equal(1, output[0].Count);
    }
}