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
[Route("api/{worldDcRegion}/{itemIds}")]
public class CurrentlyShownController : CurrentlyShownControllerBase
{
    public CurrentlyShownController(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb,
        IHistoryDbAccess historyDb) : base(gameData, currentlyShownDb, historyDb)
    {
    }

    /// <summary>
    /// Retrieves the data currently shown on the market board for the requested item and world or data center.
    /// Up to 100 item IDs can be comma-separated in order to retrieve data for multiple items at once.
    /// </summary>
    /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
    /// <param name="worldDcRegion">The world, data center, or region to retrieve data for. This may be an ID or a name. Regions should be specified as Japan, Europe, North-America, Oceania, China, or 中国.</param>
    /// <param name="listingsToReturn">The number of listings to return per item. By default, all listings will be returned.</param>
    /// <param name="entriesToReturn">The number of recent history entries to return per item. By default, a maximum of 5 entries will be returned.</param>
    /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days.</param>
    /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored.</param>
    /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned.</param>
    /// <param name="fields">
    /// A comma separated list of fields that should be included in the response, if omitted will return all fields.
    /// For example if you're only interested in the listings price per unit you can set this to listings.pricePerUnit
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="400">The parameters were invalid.</response>
    /// <response code="404">
    /// The world/DC or item requested is invalid. When requesting multiple items at once, an invalid item ID
    /// will not trigger this. Instead, the returned list of unresolved item IDs will contain the invalid item ID or IDs.
    /// </response>
    [HttpGet]
    [ApiTag("Market board current data")]
    [ProducesResponseType(typeof(CurrentlyShownView), 200)]
    [ProducesResponseType(typeof(CurrentlyShownMultiView), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(
        string itemIds,
        string worldDcRegion,
        [FromQuery(Name = "listings")] string listingsToReturn = "",
        [FromQuery(Name = "entries")] string entriesToReturn = "",
        [FromQuery] string hq = "",
        [FromQuery] string statsWithin = "",
        [FromQuery] string entriesWithin = "",
        [FromQuery] string fields = "",
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownControllerV1.Get");
        activity?.AddTag("itemIds", itemIds);
        activity?.AddTag("worldDcRegion", worldDcRegion);
        activity?.AddTag("listingsToReturn", listingsToReturn);
        activity?.AddTag("entriesToReturn", entriesToReturn);

        if (itemIds == null || worldDcRegion == null)
        {
            return BadRequest();
        }

        // Parameter parsing
        var itemIdsArray = InputProcessing.ParseIdList(itemIds)
            .Take(100)
            .ToArray();

        if (!TryGetWorldDc(worldDcRegion, out var worldDc))
        {
            return NotFound();
        }

        if (!TryGetWorldIds(worldDc, out var worldIds))
        {
            return NotFound();
        }

        var nListings = int.MaxValue;
        if (int.TryParse(listingsToReturn, out var queryListings))
        {
            nListings = Math.Max(0, queryListings);
        }

        var nEntries = 5;
        if (int.TryParse(entriesToReturn, out var queryEntries))
        {
            nEntries = Math.Min(Math.Max(0, queryEntries), 999999);
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

        bool? hqBool = string.IsNullOrEmpty(hq) || hq.ToLowerInvariant() == "null" ? null : Util.ParseUnusualBool(hq);

        var serializableProperties = BuildSerializableProperties(InputProcessing.ParseFields(fields));

        // Database logic
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        if (itemIdsArray.Length == 1)
        {
            var itemId = itemIdsArray[0];

            if (!GameData.MarketableItemIds().Contains(itemId))
            {
                return Ok(new CurrentlyShownView());
            }

            var (_, currentlyShownView) = await GetCurrentlyShownView(
                worldDc, worldIds, itemId, nListings, nEntries, hqBool, statsWithinMs, entriesWithinSeconds,
                serializableProperties,
                cts.Token);
            return Ok(currentlyShownView);
        }

        // Multi-item handling
        var itemsSerializableProperties = BuildSerializableProperties(serializableProperties, "items");
        var currentlyShownViewTasks = itemIdsArray
            .Select(itemId => GetCurrentlyShownView(
                worldDc, worldIds, itemId, nListings, nEntries, hqBool, statsWithinMs, entriesWithinSeconds,
                itemsSerializableProperties,
                cts.Token))
            .ToList();
        var currentlyShownViews = await Task.WhenAll(currentlyShownViewTasks);
        var unresolvedItems = currentlyShownViews
            .Where(cs => !cs.Item1)
            .Select(cs => cs.Item2.ItemId)
            .ToArray();
        return Ok(new CurrentlyShownMultiView
        {
            ItemIds = itemIdsArray.ToList(),
            Items = currentlyShownViews
                .Where(cs => cs.Item1)
                .Select(cs => cs.Item2)
                .ToList(),
            WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
            WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
            DcName = worldDc.IsDc ? worldDc.DcName : null,
            UnresolvedItemIds = unresolvedItems,
            SerializableProperties = serializableProperties,
        });
    }
}