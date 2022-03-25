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
    private readonly List<MostRecentlyUpdated> _store = new();
        
    public Task Create(MostRecentlyUpdated document, CancellationToken cancellationToken = default)
    {
        _store.Add(document);
        return Task.CompletedTask;
    }

    public async Task Push(uint worldId, WorldItemUpload document, CancellationToken cancellationToken = default)
    {
        var query = new MostRecentlyUpdatedQuery();
        var existing = await Retrieve(query, cancellationToken);

        if (existing == null)
        {
            await Create(new MostRecentlyUpdated
            {
                WorldId = worldId,
                Uploads = new List<WorldItemUpload> { document },
            }, cancellationToken);
            return;
        }
            
        var existingIndex = existing.Uploads.FindIndex(o => o.ItemId == document.ItemId);
        if (existingIndex != -1)
        {
            existing.Uploads.RemoveAt(existingIndex);
            existing.Uploads.Insert(0, document);
        }
        else
        {
            existing.Uploads.Insert(0, document);
            existing.Uploads = existing.Uploads.Take(MostRecentlyUpdatedDbAccess.MaxItems).ToList();
        }
    }

    public Task<MostRecentlyUpdated> Retrieve(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.FirstOrDefault(o => o.WorldId == query.WorldId));
    }

    public async Task<IList<MostRecentlyUpdated>> RetrieveMany(MostRecentlyUpdatedManyQuery query, CancellationToken cancellationToken = default)
    {
        return _store.Where(o => query.WorldIds.Contains(o.WorldId)).ToList();
    }
}