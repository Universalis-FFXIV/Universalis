using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DbAccess.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [ApiController]
    [Route("api/{worldOrDc}/{itemIds}")]
    public class CurrentlyShownController : CurrentlyShownControllerBase
    {
        public CurrentlyShownController(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb) : base(gameData, currentlyShownDb) { }

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.
        /// Item IDs can be comma-separated in order to retrieve data for multiple items at once.
        /// </summary>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">Data retrieved successfully.</response>
        /// <response code="404">
        /// The world/DC or item requested is invalid. When requesting multiple items at once, an invalid item ID
        /// will not trigger this. Instead, the returned list of unresolved item IDs will contain the invalid item ID or IDs.
        /// </response>
        [HttpGet]
        [ProducesResponseType(typeof(CurrentlyShownView), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(string itemIds, string worldOrDc, CancellationToken cancellationToken = default)
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

            if (itemIdsArray.Length == 1)
            {
                var itemId = itemIdsArray[0];

                if (!GameData.MarketableItemIds().Contains(itemId))
                {
                    return NotFound();
                }

                var (_, currentlyShownView) = await GetCurrentlyShownView(worldDc, worldIds, itemId, cancellationToken);
                return Ok(currentlyShownView);
            }

            // Multi-item handling
            var currentlyShownViewTasks = itemIdsArray
                .Select(itemId => GetCurrentlyShownView(worldDc, worldIds, itemId, cancellationToken))
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
            });
        }
    }
}
