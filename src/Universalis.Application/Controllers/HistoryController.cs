using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.GameData;

namespace Universalis.Application.Controllers
{
    [Route("api/history/{itemId}/{worldOrDc}/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;

        public HistoryController(IGameDataProvider gameData)
        {
            _gameData = gameData;
        }

        [HttpGet]
        public ActionResult<string> Get(uint itemId, string worldOrDc)
        {
            if (!_gameData.MarketableItemIds().Contains(itemId) || worldOrDc.Length == 0)
                return NotFound();

            var worldDc = WorldDc.From(worldOrDc, _gameData);

            return worldDc.IsWorld ? worldDc.WorldId.ToString() : worldDc.DcName;
        }
    }
}
