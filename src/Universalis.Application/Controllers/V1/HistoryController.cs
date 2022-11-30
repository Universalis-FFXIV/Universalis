using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1;
using Universalis.DbAccess.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1;

[ApiController]
[ApiVersion("1")]
[Route("api/history/{worldDcRegion}/{itemIds}")]
public class HistoryController : HistoryControllerBase
{
    public HistoryController(IGameDataProvider gameData, IHistoryDbAccess historyDb) : base(gameData, historyDb) { }

    /// <summary>
    /// Retrieves the history data for the requested item and world or data center.
    /// Item IDs can be comma-separated in order to retrieve data for multiple items at once.
    /// </summary>
    /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
    /// <param name="worldDcRegion">The world or data center to retrieve data for. This may be an ID or a name. Regions should be specified as Japan, Europe, North-America, Oceania, China, or 中国.</param>
    /// <param name="entriesToReturn">The number of entries to return. By default, this is set to 1800, but may be set to a maximum of 999999.</param>
    /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days.</param>
    /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="404">
    /// The world/DC or item requested is invalid. When requesting multiple items at once, an invalid item ID
    /// will not trigger this. Instead, the returned list of unresolved item IDs will contain the invalid item ID or IDs.
    /// </response>
    [HttpGet]
    [ApiTag("Market board sale history")]
    [ProducesResponseType(typeof(HistoryView), 200)]
    [ProducesResponseType(typeof(HistoryMultiView), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(
        string itemIds,
        string worldDcRegion,
        [FromQuery(Name = "entries")] string entriesToReturn,
        [FromQuery] string statsWithin = "",
        [FromQuery] string entriesWithin = "",
        CancellationToken cancellationToken = default)
    {
        // Parameter parsing
        var itemIdsArray = InputProcessing.ParseIdList(itemIds)
            .Take(100)
            .ToList();

        if (!TryGetWorldDc(worldDcRegion, out var worldDc))
        {
            return NotFound();
        }

        if (!TryGetWorldIds(worldDc, out var worldIds))
        {
            return NotFound();
        }

        var entries = 1800;
        if (int.TryParse(entriesToReturn, out var queryEntries))
        {
            entries = Math.Min(Math.Max(0, queryEntries), 999999);
        }

        var statsWithinMs = 604800000L;
        if (long.TryParse(statsWithin, out var queryStatsWithinMs))
        {
            statsWithinMs = Math.Max(0, queryStatsWithinMs);
        }

        var entriesWithinSeconds = -1L;
        if (long.TryParse(entriesWithin, out var queryEntriesWithinSeconds))
        {
            entriesWithinSeconds = Math.Max(0, queryEntriesWithinSeconds);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(5000);

        if (itemIdsArray.Count == 1)
        {
            var itemId = itemIdsArray[0];

            if (!GameData.MarketableItemIds().Contains(itemId))
            {
                return NotFound();
            }

            var (_, historyView) = await GetHistoryView(worldDc, worldIds, itemId, entries, statsWithinMs, entriesWithinSeconds, cts.Token);
            return Ok(historyView);
        }

        // Multi-item handling
        var historyViewTasks = itemIdsArray
            .Select(itemId => GetHistoryView(worldDc, worldIds, itemId, entries, statsWithinMs, entriesWithinSeconds, cts.Token))
            .ToList();
        var historyViews = await Task.WhenAll(historyViewTasks);
        var unresolvedItems = historyViews
            .Where(hv => !hv.Item1)
            .Select(hv => hv.Item2.ItemId)
            .ToArray();
        return Ok(new HistoryMultiView
        {
            ItemIds = itemIdsArray,
            Items = historyViews
                .Where(hv => hv.Item1)
                .Select(hv => hv.Item2)
                .ToList(),
            WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
            WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
            DcName = worldDc.IsDc ? worldDc.DcName : null,
            UnresolvedItemIds = unresolvedItems,
        });
    }
}