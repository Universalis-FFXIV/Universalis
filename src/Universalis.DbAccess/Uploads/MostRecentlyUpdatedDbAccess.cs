using System;
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

    public async Task<IList<WorldItemUpload>> GetMostRecent(MostRecentlyUpdatedQuery query,
        CancellationToken cancellationToken = default)
    {
        var data = await _store.GetMostRecent(query.WorldId, query.Count - 1);
        return data.Select(kvp => new WorldItemUpload
        {
            WorldId = query.WorldId,
            ItemId = kvp.Key,
            LastUploadTimeUnixMilliseconds = kvp.Value,
        }).ToList();
    }

    public async Task<IList<WorldItemUpload>> GetAllMostRecent(MostRecentlyUpdatedManyQuery query,
        CancellationToken cancellationToken = default)
    {
        // Overfetch from each world to compensate for non-valid items (e.g., non-marketable)
        // that cluster at the extremes. Scale by world count since each world may contribute
        // a block of invalid items at its extreme end.
        var perWorldCount = Math.Max(query.Count * 3, query.Count + query.WorldIds.Length * 50);

        var tasks = query.WorldIds.Select(async world =>
        {
            var worldData = await _store.GetMostRecent(world, perWorldCount - 1);
            return worldData.Select(kvp => new WorldItemUpload
            {
                WorldId = world,
                ItemId = kvp.Key,
                LastUploadTimeUnixMilliseconds = kvp.Value,
            });
        });

        var results = await Task.WhenAll(tasks);
        var data = results.SelectMany(x => x)
            .Where(d => query.ValidItemIds.Contains(d.ItemId));

        var heap = new SimplePriorityQueue<WorldItemUpload, double>(Comparer<double>.Create((a, b) => b.CompareTo(a)));
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

    public async Task<IList<WorldItemUpload>> GetLeastRecent(MostRecentlyUpdatedQuery query,
        CancellationToken cancellationToken = default)
    {
        var data = await _store.GetLeastRecent(query.WorldId, query.Count - 1);
        return data.Select(kvp => new WorldItemUpload
        {
            WorldId = query.WorldId,
            ItemId = kvp.Key,
            LastUploadTimeUnixMilliseconds = kvp.Value,
        }).ToList();
    }

    public async Task<IList<WorldItemUpload>> GetAllLeastRecent(MostRecentlyUpdatedManyQuery query,
        CancellationToken cancellationToken = default)
    {
        // Overfetch from each world to compensate for non-valid items (e.g., non-marketable)
        // that cluster at the extremes. Scale by world count since each world may contribute
        // a block of invalid items at its extreme end.
        var perWorldCount = Math.Max(query.Count * 3, query.Count + query.WorldIds.Length * 50);

        var tasks = query.WorldIds.Select(async world =>
        {
            var worldData = await _store.GetLeastRecent(world, perWorldCount - 1);
            return worldData.Select(kvp => new WorldItemUpload
            {
                WorldId = world,
                ItemId = kvp.Key,
                LastUploadTimeUnixMilliseconds = kvp.Value,
            });
        });

        var results = await Task.WhenAll(tasks);
        var data = results.SelectMany(x => x)
            .Where(d => query.ValidItemIds.Contains(d.ItemId));

        var heap = new SimplePriorityQueue<WorldItemUpload, double>(Comparer<double>.Create((a, b) => a.CompareTo(b)));
        foreach (var d in data)
        {
            // Build a min heap
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
}