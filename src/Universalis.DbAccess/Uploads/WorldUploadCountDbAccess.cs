using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class WorldUploadCountDbAccess : IWorldUploadCountDbAccess, IDisposable
{
    public static readonly string Key = "Universalis.WorldUploadCounts";

    private readonly IWorldUploadCountStore _store;

    public WorldUploadCountDbAccess(IWorldUploadCountStore store)
    {
        _store = store;
    }

    public async Task Increment(WorldUploadCountQuery query, CancellationToken cancellationToken = default)
    {
        await _store.Increment(Key, query.WorldName);
    }

    public async Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts(CancellationToken cancellationToken = default)
    {
        var data = await _store.GetWorldUploadCounts(Key);
        return data.Select(kvp => new WorldUploadCount { Count = kvp.Value, WorldName = kvp.Key })
            .OrderByDescending(w => w.Count);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}