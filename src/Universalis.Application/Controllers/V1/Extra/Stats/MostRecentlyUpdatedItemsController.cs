using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
public class MostRecentlyUpdatedItemsController : WorldDcControllerBase
{
    private readonly IMostRecentlyUpdatedDbAccess _mostRecentlyUpdatedDb;

    public MostRecentlyUpdatedItemsController(IGameDataProvider gameData,
        IMostRecentlyUpdatedDbAccess mostRecentlyUpdatedDb) : base(gameData)
    {
        _mostRecentlyUpdatedDb = mostRecentlyUpdatedDb;
    }

    /// <summary>
    /// Get the most-recently updated items on the specified world or data center, along with the upload times for each item.
    /// </summary>
    /// <param name="world">The world to request data for.</param>
    /// <param name="dcName">The data center to request data for.</param>
    /// <param name="entriesToReturn">The number of entries to return (default 50, max 200).</param>
    /// <param name="cancellationToken"></param>
    /// <response code="404">The world/DC requested is invalid.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Most-recently updated items")]
    [Route("extra/stats/most-recently-updated")]
    [ProducesResponseType(typeof(MostRecentlyUpdatedItemsView), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get([FromQuery] string world, [FromQuery] string dcName, [FromQuery(Name = "entries")] string entriesToReturn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(world) && string.IsNullOrEmpty(dcName))
        {
            return NotFound();
        }

        var worldOrDc = world;
        if (string.IsNullOrEmpty(worldOrDc))
        {
            worldOrDc = dcName;
        }

        if (!TryGetWorldDc(worldOrDc, out var worldDc))
        {
            return NotFound();
        }

        if (!TryGetWorldIds(worldDc, out var worldIds))
        {
            return NotFound();
        }

        var count = 50;
        if (int.TryParse(entriesToReturn, out var queryCount))
        {
            count = Math.Min(Math.Max(0, queryCount), 200);
        }

        var documents = await _mostRecentlyUpdatedDb.GetAllMostRecent(
            new MostRecentlyUpdatedManyQuery { WorldIds = worldIds, Count = count },
            cancellationToken);

        var worlds = GameData.AvailableWorlds();
        return Ok(new MostRecentlyUpdatedItemsView
        {
            Items = documents
                .Select(o => new WorldItemRecencyView
                {
                    WorldId = o.WorldId,
                    WorldName = worlds[o.WorldId],
                    ItemId = o.ItemId,
                    LastUploadTimeUnixMilliseconds = o.LastUploadTimeUnixMilliseconds,
                })
                .ToList(),
        });
    }
    
    /// <summary>
    /// Get the most-recently updated items on the specified world or data center, along with the upload times for each item.
    /// </summary>
    /// <param name="world">The world to request data for.</param>
    /// <param name="dcName">The data center to request data for.</param>
    /// <param name="entriesToReturn">The number of entries to return (default 50, max 200).</param>
    /// <param name="cancellationToken"></param>
    /// <response code="404">The world/DC requested is invalid.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Most-recently updated items")]
    [Route("v{version:apiVersion}/extra/stats/most-recently-updated")]
    [ProducesResponseType(typeof(MostRecentlyUpdatedItemsView), 200)]
    [ProducesResponseType(404)]
    public Task<IActionResult> GetV2([FromQuery] string world, [FromQuery] string dcName,
        [FromQuery(Name = "entries")] string entriesToReturn, CancellationToken cancellationToken = default)
    {
        return Get(world, dcName, entriesToReturn, cancellationToken);
    }
}