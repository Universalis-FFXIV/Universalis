using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.AccessControl;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockTrustedSourceDbAccess : ITrustedSourceDbAccess
{
    private readonly Dictionary<string, ApiKey> _collection = new();
    private readonly Dictionary<string, long> _uploadCounts = new();

    public Task Create(ApiKey document, CancellationToken cancellationToken = default)
    {
        _collection.Add(document.TokenSha512, document);
        _uploadCounts.Add(document.TokenSha512, 0);
        return Task.CompletedTask;
    }

    public Task<ApiKey> Retrieve(TrustedSourceQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_collection
            .FirstOrDefault(s => s.Key == query.ApiKeySha512).Value);
    }

    public Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_collection.Values
            .Select(s => new TrustedSourceNoApiKey
            {
                Name = s.Name,
                UploadCount = _uploadCounts[s.TokenSha512],
            }));
    }

    public Task Increment(TrustedSourceQuery query, CancellationToken cancellationToken = default)
    {
        var apiKey = _collection.FirstOrDefault(e => e.Key == query.ApiKeySha512);
        if (apiKey.Value == null)
        {
            return Task.CompletedTask;
        }
        
        _uploadCounts[apiKey.Key]++;
        return Task.CompletedTask;
    }

    public Task Delete(TrustedSourceQuery query, CancellationToken cancellationToken = default)
    {
        _collection.Remove(query.ApiKeySha512);
        return Task.CompletedTask;
    }
}