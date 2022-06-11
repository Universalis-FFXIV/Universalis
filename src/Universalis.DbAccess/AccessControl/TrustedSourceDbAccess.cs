using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.AccessControl;

namespace Universalis.DbAccess.AccessControl;

public class TrustedSourceDbAccess : ITrustedSourceDbAccess
{
    public static readonly string Key = "Universalis.TrustedSourceUploadCounts";
    
    private readonly IApiKeyStore _apiKeyStore;
    private readonly ISourceUploadCountStore _uploadCountStore;

    public TrustedSourceDbAccess(IApiKeyStore apiKeyStore, ISourceUploadCountStore uploadCountStore)
    {
        _apiKeyStore = apiKeyStore;
        _uploadCountStore = uploadCountStore;
    }

    public Task Create(ApiKey document, CancellationToken cancellationToken = default)
    {
        return _apiKeyStore.Insert(document, cancellationToken);
    }

    public Task<ApiKey> Retrieve(TrustedSourceQuery query, CancellationToken cancellationToken = default)
    {
        return _apiKeyStore.Retrieve(query.ApiKeySha512, cancellationToken);
    }

    public async Task Increment(TrustedSourceQuery query, CancellationToken cancellationToken = default)
    {
        var apiKey = await Retrieve(query, cancellationToken);
        if (apiKey == null)
        {
            return;
        }
        
        await _uploadCountStore.IncrementCounter(Key, apiKey.Name);
    }

    public async Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts(CancellationToken cancellationToken = default)
    {
        var counts = await _uploadCountStore.GetCounterValues(Key);
        return counts.Select(kvp => new TrustedSourceNoApiKey
        {
            Name = kvp.Key,
            UploadCount = kvp.Value,
        });
    }
}