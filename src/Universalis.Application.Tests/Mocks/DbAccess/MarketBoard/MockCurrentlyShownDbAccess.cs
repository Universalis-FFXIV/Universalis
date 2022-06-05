using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;

public class MockCurrentlyShownDbAccess : ICurrentlyShownDbAccess
{
    private readonly List<CurrentlyShownSimple> _collection = new();

    private Task Create(CurrentlyShownSimple document, CancellationToken cancellationToken = default)
    {
        _collection.Add(document);
        return Task.CompletedTask;
    }

    public Task<CurrentlyShownSimple> Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_collection
            .FirstOrDefault(d => d.WorldId == query.WorldId && d.ItemId == query.ItemId));
    }

    public async Task Update(CurrentlyShownSimple document, CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        await Delete(query, cancellationToken);
        await Create(document, cancellationToken);
    }

    private async Task Delete(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        var document = await Retrieve(query, cancellationToken);
        _collection.Remove(document);
    }
}