using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
public class RecentlyUpdatedItemsController : ControllerBase
{
    private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;
    
    public RecentlyUpdatedItemsController(IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb)
    {
        _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
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
        var data = (await _recentlyUpdatedItemsDb.Retrieve(cancellationToken))?.Items;
        return data == null
            ? new RecentlyUpdatedItemsView { Items = new List<uint>() }
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