using Microsoft.AspNetCore.Mvc;
using System.Linq;
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

        [HttpGet]
        public async Task<IActionResult> Get(string itemIds, string worldOrDc)
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

                var (_, currentlyShownView) = await GetCurrentlyShownView(worldDc, worldIds, itemId);
                return Ok(currentlyShownView);
            }

            // Multi-item handling
            var currentlyShownViewTasks = itemIdsArray
                .Select(itemId => GetCurrentlyShownView(worldDc, worldIds, itemId))
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
