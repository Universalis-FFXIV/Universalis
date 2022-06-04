using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class MostRecentlyUpdatedDbAccess : IMostRecentlyUpdatedDbAccess
{
    public static readonly int MaxItems = 200;

    // TODO: This can be used to get the least-recently-updated items, too
    public static readonly string KeyFormat = "Universalis.WorldItemUploadTimes.{0}";

    private readonly IWorldItemUploadStore _store;

    public MostRecentlyUpdatedDbAccess(IWorldItemUploadStore store)
    {
        _store = store;
    }

    public Task Push(uint worldId, WorldItemUpload document, CancellationToken cancellationToken = default)
    {
        return _store.SetItem(string.Format(KeyFormat, worldId), document.ItemId,
            document.LastUploadTimeUnixMilliseconds);
    }

    public async Task<IList<WorldItemUpload>> GetMostRecent(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _store.GetMostRecent(string.Format(KeyFormat, query.WorldId), MaxItems - 1);
        return data.Select(kvp => new WorldItemUpload
        {
            WorldId = query.WorldId,
            ItemId = kvp.Key,
            LastUploadTimeUnixMilliseconds = kvp.Value,
        }).ToList();
    }

    public async Task<IList<WorldItemUpload>> GetAllMostRecent(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
    {
        return await query.WorldIds.ToAsyncEnumerable()
            .SelectManyAwait(async world =>
            {
                return (await _store.GetMostRecent(string.Format(KeyFormat, world), MaxItems - 1))
                    .ToAsyncEnumerable()
                    .Select(kvp => new WorldItemUpload
                    {
                        WorldId = world,
                        ItemId = kvp.Key,
                        LastUploadTimeUnixMilliseconds = kvp.Value,
                    });
            })
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IList<WorldItemUpload>> GetLeastRecent(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _store.GetLeastRecent(string.Format(KeyFormat, query.WorldId), MaxItems - 1);
        return data.Select(kvp => new WorldItemUpload
        {
            WorldId = query.WorldId,
            ItemId = kvp.Key,
            LastUploadTimeUnixMilliseconds = kvp.Value,
        }).ToList();
    }

    public async Task<IList<WorldItemUpload>> GetAllLeastRecent(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
    {
        return await query.WorldIds.ToAsyncEnumerable()
            .SelectManyAwait(async world =>
            {
                return (await _store.GetLeastRecent(string.Format(KeyFormat, world), MaxItems - 1))
                    .ToAsyncEnumerable()
                    .Select(kvp => new WorldItemUpload
                    {
                        WorldId = world,
                        ItemId = kvp.Key,
                        LastUploadTimeUnixMilliseconds = kvp.Value,
                    });
            })
            .ToListAsync(cancellationToken);
    }
}