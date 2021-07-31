using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DbAccess.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V2
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/{v:apiVersion}/history/{itemIds}/{worldOrDc}")]
    public class HistoryController : V1.HistoryController
    {
        public HistoryController(IGameDataProvider gameData, IHistoryDbAccess historyDb) : base(gameData, historyDb) { }

        [HttpGet]
        public new async Task<IActionResult> Get(string itemIds, string worldOrDc, [FromQuery] string entriesToReturn)
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

            var entries = 1800;
            if (int.TryParse(entriesToReturn, out var queryEntries))
            {
                entries = Math.Min(Math.Max(0, queryEntries), 999999);
            }

            if (itemIdsArray.Length == 1)
            {
                var itemId = itemIdsArray[0];

                if (!GameData.MarketableItemIds().Contains(itemId))
                {
                    return NotFound();
                }

                var (_, historyView) = await GetHistoryView(worldDc, worldIds, itemId, entries);
                return Ok(historyView);
            }

            // Multi-item handling
            var historyViewTasks = itemIdsArray
                .Select(itemId => GetHistoryView(worldDc, worldIds, itemId, entries))
                .ToList();
            var historyViews = await Task.WhenAll(historyViewTasks);
            var unresolvedItems = historyViews
                .Where(hv => !hv.Item1)
                .Select(hv => hv.Item2.ItemId)
                .ToArray();
            return Ok(new HistoryMultiViewV2
            {
                ItemIds = itemIdsArray.ToList(),
                Items = historyViews
                    .Where(cs => cs.Item1)
                    .Select(hv => hv.Item2)
                    .ToDictionary(item => item.ItemId, item => item),
                WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
                WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
                DcName = worldDc.IsDc ? worldDc.DcName : null,
                UnresolvedItemIds = unresolvedItems,
            });
        }
    }
}