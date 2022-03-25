using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.Entities;

namespace Universalis.Application.Tests.Mocks.DbAccess;

public class MockContentDbAccess : IContentDbAccess
{
    private readonly Dictionary<string, Content> _collection = new();

    public Task Create(Content document, CancellationToken cancellationToken = default)
    {
        _collection.Add(document.ContentId, document);
        return Task.CompletedTask;
    }

    public Task<Content> Retrieve(ContentQuery query, CancellationToken cancellationToken = default)
    {
        return !_collection.TryGetValue(query.ContentId ?? "", out var taxRates)
            ? Task.FromResult<Content>(null)
            : Task.FromResult(taxRates);
    }

    public async Task Update(Content document, ContentQuery query, CancellationToken cancellationToken = default)
    {
        await Delete(query, cancellationToken);
        await Create(document, cancellationToken);
    }

    public Task Delete(ContentQuery query, CancellationToken cancellationToken = default)
    {
        _collection.Remove(query.ContentId);
        return Task.CompletedTask;
    }
}