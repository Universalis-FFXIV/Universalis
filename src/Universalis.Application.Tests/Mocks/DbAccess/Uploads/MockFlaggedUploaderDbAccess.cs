using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockFlaggedUploaderDbAccess : IFlaggedUploaderDbAccess
{
    private readonly Dictionary<string, FlaggedUploader> _collection = new();

    public Task Create(FlaggedUploader document, CancellationToken cancellationToken = default)
    {
        _collection.Add(document.UploaderIdHash, document);
        return Task.CompletedTask;
    }

    public Task<FlaggedUploader> Retrieve(FlaggedUploaderQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_collection
            .FirstOrDefault(s => s.Key == query.UploaderIdHash).Value);
    }

    public async Task Update(FlaggedUploader document, FlaggedUploaderQuery query, CancellationToken cancellationToken = default)
    {
        await Delete(query, cancellationToken);
        await Create(document, cancellationToken);
    }

    public Task Delete(FlaggedUploaderQuery query, CancellationToken cancellationToken = default)
    {
        _collection.Remove(query.UploaderIdHash);
        return Task.CompletedTask;
    }
}