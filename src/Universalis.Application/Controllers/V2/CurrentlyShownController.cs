using System;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DbAccess.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V2
{
    [ApiController]
    [ApiVersion("2")]
    [Route("api/v{version:apiVersion}/{worldOrDc}/{itemIds}")]
    public class CurrentlyShownController : CurrentlyShownControllerBase
    {
        public CurrentlyShownController(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb) : base(gameData, currentlyShownDb) { }

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.
        /// Item IDs can be comma-separated in order to retrieve data for multiple items at once.
        /// </summary>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listingsToReturn">The number of listings to return. By default, all listings will be returned.</param>
        /// <param name="entriesToReturn">The number of entries to return. By default, all entries will be returned.</param>
        /// <param name="noGst">
        /// If the result should not have Gil sales tax (GST) factored in. GST is applied to all
        /// consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.
        /// By default, GST is factored in. Set this parameter to true or 1 to prevent this.
        /// </param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">Data retrieved successfully.</response>
        /// <response code="404">
        /// The world/DC or item requested is invalid. When requesting multiple items at once, an invalid item ID
        /// will not trigger this. Instead, the returned list of unresolved item IDs will contain the invalid item ID or IDs.
        /// </response>
        [HttpGet]
        [ProducesResponseType(typeof(CurrentlyShownView), 200)]
        [ProducesResponseType(typeof(CurrentlyShownMultiViewV2), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(string itemIds, string worldOrDc, [FromQuery(Name = "listings")] string listingsToReturn = "", [FromQuery(Name = "entries")] string entriesToReturn = "", [FromQuery] string noGst = "", CancellationToken cancellationToken = default)
        {
            // Parameter parsing
            var itemIdsArray = InputProcessing.ParseIdList(itemIds)
                .Take(100)
                .ToArray();

            if (!TryGetWorldDc(worldOrDc, out var worldDc))
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

            var nEntries = int.MaxValue;
            if (int.TryParse(entriesToReturn, out var queryEntries))
            {
                nEntries = Math.Max(0, queryEntries);
            }

            var noGstBool = Util.ParseUnusualBool(noGst);

            if (itemIdsArray.Length == 1)
            {
                var itemId = itemIdsArray[0];

                if (!GameData.MarketableItemIds().Contains(itemId))
                {
                    return NotFound();
                }

                var (_, currentlyShownView) = await GetCurrentlyShownView(worldDc, worldIds, itemId, nListings, nEntries, noGstBool, cancellationToken);
                return Ok(currentlyShownView);
            }

            // Multi-item handling
            var currentlyShownViewTasks = itemIdsArray
                .Select(itemId => GetCurrentlyShownView(worldDc, worldIds, itemId, nListings, nEntries, noGstBool, cancellationToken))
                .ToList();
            var currentlyShownViews = await Task.WhenAll(currentlyShownViewTasks);
            var unresolvedItems = currentlyShownViews
                .Where(cs => !cs.Item1)
                .Select(cs => cs.Item2.ItemId)
                .ToArray();
            return Ok(new CurrentlyShownMultiViewV2
            {
                ItemIds = itemIdsArray.ToList(),
                Items = currentlyShownViews
                    .Where(cs => cs.Item1)
                    .Select(cs => cs.Item2)
                    .ToDictionary(item => item.ItemId, item => item),
                WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
                WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
                DcName = worldDc.IsDc ? worldDc.DcName : null,
                UnresolvedItemIds = unresolvedItems,
            });
        }
    }
}