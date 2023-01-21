using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
public class RecentlyUpdatedItemsController : ControllerBase
{
    private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;
    private readonly IGameDataProvider _gameData;
    
    public RecentlyUpdatedItemsController(IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb, IGameDataProvider gameData)
    {
        _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
        _gameData = gameData;
    }

    /// <summary>
    /// Returns a list of some of the most recently updated items on the website. This endpoint
    /// is a legacy endpoint and does not include any data on which worlds or data centers the updates happened on.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Recently updated items")]
    [Route("extra/stats/recently-updated")]
    [ProducesResponseType(typeof(RecentlyUpdatedItemsView), 200)]
    public async Task<RecentlyUpdatedItemsView> Get(CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        var marketable = _gameData.MarketableItemIds();
        var data = (await _recentlyUpdatedItemsDb.Retrieve(cts.Token))?.Items
            .Intersect(marketable)
            .ToList();
        return data == null
            ? new RecentlyUpdatedItemsView { Items = new List<int>() }
            : new RecentlyUpdatedItemsView { Items = data };
    }

    /// <summary>
    /// Returns a list of some of the most recently updated items on the website. This endpoint
    /// is a legacy endpoint and does not include any data on which worlds or data centers the updates happened on.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Recently updated items")]
    [Route("v{version:apiVersion}/extra/stats/recently-updated")]
    [ProducesResponseType(typeof(RecentlyUpdatedItemsView), 200)]
    public Task<RecentlyUpdatedItemsView> GetV2(CancellationToken cancellationToken = default)
    {
        return Get(cancellationToken);
    }
}