using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[Route("api/extra/stats/recently-updated-2")]
public class RUIC2 : ControllerBase
{
    private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;
    
    public RUIC2(IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb)
    {
        _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
    }

    /// <summary>
    /// Returns a list of some of the most recently updated items on the website. This endpoint
    /// is a legacy endpoint and does not include any data on which worlds or data centers the updates happened on.
    /// </summary>
    [HttpGet]
    [ApiTag("Recently updated items")]
    [ProducesResponseType(typeof(RecentlyUpdatedItemsView), 200)]
    public async Task<RecentlyUpdatedItemsView> Get(CancellationToken cancellationToken = default)
    {
        List<uint> data = null;
        try
        {
            data = (await _recentlyUpdatedItemsDb.Retrieve(new RecentlyUpdatedItemsQuery(), cancellationToken))
                ?.Items;
        }
        catch (Exception)
        {
            // ignored
        }
        
        return data == null
            ? new RecentlyUpdatedItemsView { Items = new List<uint> { 5 } }
            : new RecentlyUpdatedItemsView { Items = data };
    }
}