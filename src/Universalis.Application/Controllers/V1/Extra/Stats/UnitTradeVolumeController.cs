using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
public class UnitTradeVolumeController : WorldDcRegionControllerBase
{
    private readonly ISaleStatisticsDbAccess _saleStatistics;

    public UnitTradeVolumeController(ISaleStatisticsDbAccess saleStatistics, IGameDataProvider gameData) : base(gameData)
    {
        _saleStatistics = saleStatistics;
    }

    /// <summary>
    /// Retrieves the unit sale volume (total units sold) of the specified item over the provided period.
    /// </summary>
    /// <param name="world">The world to query.</param>
    /// <param name="dcName">The data center to query.</param>
    /// <param name="item">The ID of the item to query.</param>
    /// <param name="from">The time, in milliseconds since the UNIX epoch, to begin the interval over.</param>
    /// <param name="to">
    /// The time, in milliseconds since the UNIX epoch, to end the interval over. If this is not provided, it
    /// will be set to the current time.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <response code="400">No world or data center was provided.</response>
    /// <response code="404">The world/DC/item requested is invalid.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Unit trade volume")]
    [Route("extra/stats/unit-trade-volume")]
    [ProducesResponseType(typeof(UnitTradeVolumeView), 200)]
    public async Task<IActionResult> Get(
        [FromQuery] string world,
        [FromQuery] string dcName,
        [FromQuery, BindRequired] uint item,
        [FromQuery, BindRequired] long from,
        [FromQuery] long to = -1,
        CancellationToken cancellationToken = default)
    {
        if (!GameData.MarketableItemIds().Contains(item))
        {
            return NotFound("item is not marketable");
        }

        if (to == -1)
        {
            to = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        if (string.IsNullOrEmpty(world) && string.IsNullOrEmpty(dcName))
        {
            return BadRequest("no world or data center provided");
        }

        var worldOrDc = world;
        if (string.IsNullOrEmpty(worldOrDc))
        {
            worldOrDc = dcName;
        }

        if (!TryGetWorldDc(worldOrDc, out var worldDc))
        {
            return NotFound("world or data center not found");
        }

        if (!TryGetWorldIds(worldDc, out var worldIds))
        {
            return NotFound("world or data center not found");
        }

        var fromTime = DateTimeOffset.FromUnixTimeMilliseconds(from).UtcDateTime;
        var toTime = DateTimeOffset.FromUnixTimeMilliseconds(to).UtcDateTime;
        var quantity = await worldIds.ToAsyncEnumerable()
            .SelectAwaitWithCancellation((w, ct) => _saleStatistics.RetrieveUnitTradeVolume(new UnitTradeVolumeQuery
            {
                WorldId = w,
                ItemId = item,
                From = fromTime,
                To = toTime,
            }, ct))
            .SumAsync(cancellationToken);
        return Ok(new UnitTradeVolumeView
        {
            Quantity = quantity,
            From = new DateTimeOffset(fromTime).ToUnixTimeMilliseconds(),
            To = new DateTimeOffset(toTime).ToUnixTimeMilliseconds(),
        });
    }

    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Unit trade volume")]
    [Route("v{version:apiVersion}/extra/stats/unit-trade-volume")]
    [ProducesResponseType(typeof(UnitTradeVolumeView), 200)]
    public Task<IActionResult> GetV2(
        [FromQuery] string world,
        [FromQuery] string dcName,
        [FromQuery] uint item,
        [FromQuery] long from,
        [FromQuery] long to = -1,
        CancellationToken cancellationToken = default)
    {
        return Get(world, dcName, item, from, to, cancellationToken);
    }
}
