using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class WorldUploadCountDbAccess : IWorldUploadCountDbAccess
{
    public static readonly string Key = "Universalis.WorldUploadCounts";
    
    private readonly IWorldUploadCountStore _store;

    public WorldUploadCountDbAccess(IWorldUploadCountStore store)
    {
        _store = store;
    }

    public Task Increment(WorldUploadCountQuery query, CancellationToken cancellationToken = default)
    {
        return _store.Increment(Key, query.WorldName);
    }

    public async Task<IEnumerable<WorldUploadCount>> GetWorldUploadCounts(CancellationToken cancellationToken = default)
    {
        var counts = await _store.GetWorldUploadCounts(Key);
        return counts.Select(c => new WorldUploadCount
        {
            WorldName = c.Key,
            Count = c.Value,
        });
    }
}