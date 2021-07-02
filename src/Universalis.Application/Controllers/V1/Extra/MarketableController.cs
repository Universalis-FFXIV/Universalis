using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1.Extra
{
    [Route("api/marketable")]
    [ApiController]
    public class MarketableController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;

        public MarketableController(IGameDataProvider gameData)
        {
            _gameData = gameData;
        }

        [HttpGet]
        public IEnumerable<uint> Get()
        {
            return _gameData.MarketableItemIds();
        }
    }
}
