﻿using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownDbAccess : ICurrentlyShownDbAccess
{
    private readonly ICurrentlyShownStore _store;

    public CurrentlyShownDbAccess(ICurrentlyShownStore store)
    {
        _store = store;
    }

    async Task<CurrentlyShownSimple> ICurrentlyShownDbAccess.Retrieve(CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _store.GetData(query.WorldId, query.ItemId);
        return data.LastUploadTimeUnixMilliseconds == 0 ? null : data;
    }

    public Task Update(CurrentlyShownSimple document, CurrentlyShownQuery query, CancellationToken cancellationToken = default)
    {
        return _store.SetData(document);
    }
}