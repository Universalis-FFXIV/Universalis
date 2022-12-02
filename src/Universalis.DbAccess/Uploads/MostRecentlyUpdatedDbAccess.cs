using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Priority_Queue;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class MostRecentlyUpdatedDbAccess : IMostRecentlyUpdatedDbAccess
{
    private readonly IWorldItemUploadStore _store;

    public MostRecentlyUpdatedDbAccess(IWorldItemUploadStore store)
    {
        _store = store;
    }

    public Task Push(int worldId, WorldItemUpload document, CancellationToken cancellationToken = default)
    {
        return _store.SetItem(worldId, document.ItemId,
            document.LastUploadTimeUnixMilliseconds);
    }

    public async Task<IList<WorldItemUpload>> GetMostRecent(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _store.GetMostRecent(query.WorldId, query.Count - 1);
        return data.Select(kvp => new WorldItemUpload
        {
            WorldId = query.WorldId,
            ItemId = kvp.Key,
            LastUploadTimeUnixMilliseconds = kvp.Value,
        }).ToList();
    }

    public async Task<IList<WorldItemUpload>> GetAllMostRecent(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
    {
        var data = await query.WorldIds.ToAsyncEnumerable()
            .SelectManyAwait(async world =>
            {
                return (await _store.GetMostRecent(world, query.Count - 1))
                    .ToAsyncEnumerable()
                    .Select(kvp => new WorldItemUpload
                    {
                        WorldId = world,
                        ItemId = kvp.Key,
                        LastUploadTimeUnixMilliseconds = kvp.Value,
                    });
            })
            .ToListAsync(cancellationToken);
        
        var heap = new SimplePriorityQueue<WorldItemUpload, double>(Comparer<double>.Create((a, b) => (int)(b - a)));
        foreach (var d in data)
        {
            // Build a heap
            heap.Enqueue(d, d.LastUploadTimeUnixMilliseconds);
        }

        var outData = new List<WorldItemUpload>();
        while (outData.Count < query.Count)
        {
            if (heap.Count == 0) break;

            // Pull the top K documents
            outData.Add(heap.First);
            heap.Dequeue();
        }

        return outData;
    }
    
    public async Task<IList<WorldItemUpload>> GetLeastRecent(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _store.GetLeastRecent(query.WorldId, query.Count - 1);
        return data.Select(kvp => new WorldItemUpload
        {
            WorldId = query.WorldId,
            ItemId = kvp.Key,
            LastUploadTimeUnixMilliseconds = kvp.Value,
        }).ToList();
    }

    public async Task<IList<WorldItemUpload>> GetAllLeastRecent(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
    {
        var data = await query.WorldIds.ToAsyncEnumerable()
            .SelectManyAwait(async world =>
            {
                return (await _store.GetLeastRecent(world, query.Count - 1))
                    .ToAsyncEnumerable()
                    .Select(kvp => new WorldItemUpload
                    {
                        WorldId = world,
                        ItemId = kvp.Key,
                        LastUploadTimeUnixMilliseconds = kvp.Value,
                    });
            })
            .ToListAsync(cancellationToken);
        
        var heap = new SimplePriorityQueue<WorldItemUpload, double>(Comparer<double>.Create((a, b) => (int)(b - a)));
        foreach (var d in data)
        {
            // Build a heap but make the timestamp negative to reverse it
            heap.Enqueue(d, -d.LastUploadTimeUnixMilliseconds);
        }

        var outData = new List<WorldItemUpload>();
        while (outData.Count < query.Count)
        {
            if (heap.Count == 0) break;

            // Pull the top K documents
            outData.Add(heap.First);
            heap.Dequeue();
        }

        return outData;
    }
}