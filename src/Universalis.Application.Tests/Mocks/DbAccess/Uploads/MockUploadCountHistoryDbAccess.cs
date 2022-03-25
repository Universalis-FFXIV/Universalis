using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockUploadCountHistoryDbAccess : IUploadCountHistoryDbAccess
{
    private readonly List<UploadCountHistory> _collection = new();

    public Task Create(UploadCountHistory document, CancellationToken cancellationToken = default)
    {
        _collection.Add(document);
        return Task.CompletedTask;
    }

    public Task<UploadCountHistory> Retrieve(UploadCountHistoryQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_collection.FirstOrDefault());
    }

    public async Task Update(UploadCountHistory document, UploadCountHistoryQuery query, CancellationToken cancellationToken = default)
    {
        await Delete(query, cancellationToken);
        await Create(document, cancellationToken);
    }

    public async Task Update(double lastPush, List<double> uploadCountByDay, CancellationToken cancellationToken = default)
    {
        var existing = await Retrieve(new UploadCountHistoryQuery(), cancellationToken);
        if (existing != null)
        {
            existing.LastPush = lastPush;
            existing.UploadCountByDay = uploadCountByDay;
            return;
        }

        await Create(new UploadCountHistory
        {
            LastPush = lastPush,
            UploadCountByDay = uploadCountByDay,
        }, cancellationToken);
    }

    public Task Delete(UploadCountHistoryQuery query, CancellationToken cancellationToken = default)
    {
        _collection.Remove(_collection.FirstOrDefault());
        return Task.CompletedTask;
    }
}