using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[Route("api/extra/stats/recently-updated")]
public class RecentlyUpdatedItemsController : ControllerBase
{
    //private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;
    
    public RecentlyUpdatedItemsController(/*IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb*/)
    {
        //_recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
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
        // try
        // {
        //     data = (await _recentlyUpdatedItemsDb.Retrieve(new RecentlyUpdatedItemsQuery(), cancellationToken))
        //         ?.Items;
        // }
        // catch (Exception)
        // {
        //     // ignored
        // }
        
        return data == null
            ? new RecentlyUpdatedItemsView { Items = new List<uint>() }
            : new RecentlyUpdatedItemsView { Items = data };
    }
}