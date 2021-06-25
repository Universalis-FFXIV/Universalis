using Microsoft.AspNetCore.Mvc;
using Universalis.GameData;

namespace Universalis.Application.Controllers
{
    [Route("api/{itemId}/{worldOrDc}/[controller]")]
    [ApiController]
    public class CurrentlyShownController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;

        public CurrentlyShownController(IGameDataProvider gameData)
        {
            _gameData = gameData;
        }

        [HttpGet]
        public ActionResult<string> Get(uint itemId, string worldOrDc)
        {
            if (!_gameData.MarketableItemIds().Contains(itemId))
                return NotFound();

            return worldOrDc;
        }
    }
}
