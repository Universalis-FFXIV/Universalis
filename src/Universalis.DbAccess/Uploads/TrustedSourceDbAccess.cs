using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class TrustedSourceDbAccess : DbAccessService<TrustedSource, TrustedSourceQuery>, ITrustedSourceDbAccess
{
    public static readonly string Key = "Universalis.TrustedSourceUploadCounts";
    
    private readonly ISourceUploadCountStore _store;

    public TrustedSourceDbAccess(IMongoClient client, ISourceUploadCountStore store) : base(client, Constants.DatabaseName,
        "trustedSources")
    {
        _store = store;
    }

    public TrustedSourceDbAccess(IMongoClient client, string databaseName, ISourceUploadCountStore store) : base(client,
        databaseName, "content")
    {
        _store = store;
    }

    public Task Increment(string sourceName, CancellationToken cancellationToken = default)
    {
        return _store.IncrementCounter(Key, sourceName);
    }

    public async Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts(CancellationToken cancellationToken = default)
    {
        var counts = await _store.GetCounterValues(Key);
        return counts.Select(kvp => new TrustedSourceNoApiKey
        {
            Name = kvp.Key,
            UploadCount = kvp.Value,
        });
    }
}