using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1.Extra.Stats
{
    [ApiController]
    [Route("api/extra/stats/most-recently-updated")]
    public class MostRecentlyUpdatedItemsController : WorldDcControllerBase
    {
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;

        public MostRecentlyUpdatedItemsController(IGameDataProvider gameData,
            ICurrentlyShownDbAccess currentlyShownDb) : base(gameData)
        {
            _currentlyShownDb = currentlyShownDb;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string world, [FromQuery] string dcName, [FromQuery(Name = "entries")] string entriesToReturn)
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
                return NotFound(worldOrDc);
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

            var documents = await _currentlyShownDb.RetrieveByUploadTime(
                new CurrentlyShownWorldIdsQuery { WorldIds = worldIds },
                count,
                UploadOrder.MostRecent);

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
    }
}