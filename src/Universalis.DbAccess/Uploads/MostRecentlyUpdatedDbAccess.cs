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

    public static readonly string KeyFormat = "Universalis.WorldItemUploads.{0}";

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

    public async Task<MostRecentlyUpdated> Retrieve(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _store.GetAllItems(string.Format(KeyFormat, query.WorldId), MaxItems - 1);
        return new MostRecentlyUpdated
        {
            WorldId = query.WorldId,
            Uploads = data.Select(kvp => new WorldItemUpload
            {
                WorldId = query.WorldId,
                ItemId = kvp.Key,
                LastUploadTimeUnixMilliseconds = kvp.Value,
            }).ToList(),
        };
    }

    public async Task<IList<MostRecentlyUpdated>> RetrieveMany(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
    {
        return await query.WorldIds.ToAsyncEnumerable()
            .SelectAwait(async world => new MostRecentlyUpdated
            {
                WorldId = world,
                Uploads = (await _store.GetAllItems(string.Format(KeyFormat, world), MaxItems - 1))
                    .Select(kvp => new WorldItemUpload
                    {
                        WorldId = world,
                        ItemId = kvp.Key,
                        LastUploadTimeUnixMilliseconds = kvp.Value,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);
    }
}