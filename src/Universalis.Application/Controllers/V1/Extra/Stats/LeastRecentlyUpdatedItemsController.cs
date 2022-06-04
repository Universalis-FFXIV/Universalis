using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[Route("api/extra/stats/least-recently-updated")]
public class LeastRecentlyUpdatedItemsController : WorldDcControllerBase
{
    private readonly IMostRecentlyUpdatedDbAccess _mostRecentlyUpdatedDb;

    public LeastRecentlyUpdatedItemsController(IGameDataProvider gameData,
        IMostRecentlyUpdatedDbAccess mostRecentlyUpdatedDb) : base(gameData)
    {
        _mostRecentlyUpdatedDb = mostRecentlyUpdatedDb;
    }

    /// <summary>
    /// Get the least-recently updated items on the specified world or data center, along with the upload times for each item.
    /// This endpoint is slow, which may require setting an especially long timeout when making requests to it. Data on this
    /// endpoint is cached for 10 minutes.
    /// </summary>
    /// <param name="world">The world to request data for.</param>
    /// <param name="dcName">The data center to request data for.</param>
    /// <param name="entriesToReturn">The number of entries to return (default 50, max 200).</param>
    /// <param name="cancellationToken"></param>
    /// <response code="404">The world/DC requested is invalid.</response>
    [HttpGet]
    [ApiTag("Least-recently updated items")]
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

        var documents = await _mostRecentlyUpdatedDb.GetAllLeastRecent(
            new MostRecentlyUpdatedManyQuery { WorldIds = worldIds, Count = count },
            cancellationToken);

        var worlds = GameData.AvailableWorlds();
        return Ok(new LeastRecentlyUpdatedItemsView
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
}