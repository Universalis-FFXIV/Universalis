using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Universalis.Application.Swagger;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[ApiVersion("3")]
[Route("api")]
public class MarketableController : ControllerBase
{
    private readonly IGameDataProvider _gameData;

    public MarketableController(IGameDataProvider gameData)
    {
        _gameData = gameData;
    }

    /// <summary>
    /// Returns the set of marketable item IDs.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Marketable items")]
    [Route("marketable")]
    [ProducesResponseType(typeof(IEnumerable<uint>), 200)]
    public IEnumerable<uint> Get()
    {
        return _gameData.MarketableItemIds();
    }
    
    /// <summary>
    /// Returns the set of marketable item IDs.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Marketable items")]
    [Route("v{version:apiVersion}/marketable")]
    [ProducesResponseType(typeof(IEnumerable<uint>), 200)]
    public IEnumerable<uint> GetV2()
    {
        return Get();
    }
    
    /// <summary>
    /// Returns the set of marketable item IDs.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("3")]
    [ApiTag("Marketable items")]
    [Route("v{version:apiVersion}/game/marketable-items")]
    [ProducesResponseType(typeof(IEnumerable<uint>), 200)]
    public IEnumerable<uint> GetV3()
    {
        return Get();
    }
}