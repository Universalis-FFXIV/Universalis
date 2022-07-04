using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V3.Game;
using Universalis.GameData;
using World = Universalis.Application.Views.V3.Game.World;

namespace Universalis.Application.Controllers.V3.Game;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[ApiVersion("3")]
[Route("api")]
public class WorldsController
{
    private readonly IGameDataProvider _gameData;

    public WorldsController(IGameDataProvider gameData)
    {
        _gameData = gameData;
    }
    
    /// <summary>
    /// Returns the IDs and names of all worlds supported by the API.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Available worlds")]
    [Route("worlds")]
    [ProducesResponseType(typeof(IEnumerable<World>), 200)]
    public IEnumerable<World> Get()
    {
        return GetV3();
    }

    /// <summary>
    /// Returns the IDs and names of all worlds supported by the API.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Available worlds")]
    [Route("v{version:apiVersion}/worlds")]
    [ProducesResponseType(typeof(IEnumerable<World>), 200)]
    public IEnumerable<World> GetV2()
    {
        return GetV3();
    }

    /// <summary>
    /// Returns the IDs and names of all worlds supported by the API.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("3")]
    [ApiTag("Available worlds")]
    [Route("v{version:apiVersion}/game/worlds")]
    [ProducesResponseType(typeof(IEnumerable<World>), 200)]
    public IEnumerable<World> GetV3()
    {
        return _gameData.AvailableWorlds().Select(kvp => new World
        {
            Id = kvp.Key,
            Name = kvp.Value,
        });
    }
}