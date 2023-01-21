using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockMostRecentlyUpdatedDbAccess : IMostRecentlyUpdatedDbAccess
{
    private readonly List<WorldItemUpload> _store = new();

    public Task Push(int worldId, WorldItemUpload document, CancellationToken cancellationToken = default)
    {
        var existingIndex = _store.FindIndex(o => o.WorldId == worldId && o.ItemId == document.ItemId);
        if (existingIndex != -1)
        {
            _store.RemoveAt(existingIndex);
        }

        _store.Insert(0, document);

        return Task.CompletedTask;
    }

    public Task<IList<WorldItemUpload>> GetMostRecent(MostRecentlyUpdatedQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((IList<WorldItemUpload>)_store.Where(o => o.WorldId == query.WorldId).ToList());
    }

    public Task<IList<WorldItemUpload>> GetAllMostRecent(MostRecentlyUpdatedManyQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Where(o => query.WorldIds.Contains(o.WorldId)).ToList();
        return Task.FromResult((IList<WorldItemUpload>)result);
    }

    public Task<IList<WorldItemUpload>> GetLeastRecent(MostRecentlyUpdatedQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            (IList<WorldItemUpload>)_store.Where(o => o.WorldId == query.WorldId).Reverse().ToList());
    }

    public Task<IList<WorldItemUpload>> GetAllLeastRecent(MostRecentlyUpdatedManyQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Where(o => query.WorldIds.Contains(o.WorldId)).Reverse().ToList();
        return Task.FromResult((IList<WorldItemUpload>)result);
    }
}