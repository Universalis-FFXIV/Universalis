using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/{worldOrDc}/{itemIds}")]
    [ApiController]
    public class CurrentlyShownController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;

        public CurrentlyShownController(IGameDataProvider gameData)
        {
            _gameData = gameData;
        }

        [HttpGet]
        public ActionResult<string> Get(string itemIds, string worldOrDc)
        {
            var itemIdsArray = itemIds.Split(',').Select(uint.Parse).ToArray();
            var itemId = itemIdsArray[0];

            if (!_gameData.MarketableItemIds().Contains(itemId) || worldOrDc.Length == 0)
                return NotFound();

            var worldDc = WorldDc.From(worldOrDc, _gameData);

            return worldDc.IsWorld ? worldDc.WorldId.ToString() : worldDc.DcName;
        }
    }
}
