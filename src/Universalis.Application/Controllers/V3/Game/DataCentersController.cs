using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.GameData;
using DataCenter = Universalis.Application.Views.V3.Game.DataCenter;

namespace Universalis.Application.Controllers.V3.Game;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[ApiVersion("3")]
[Route("api")]
public class DataCentersController
{
    private readonly IGameDataProvider _gameData;

    public DataCentersController(IGameDataProvider gameData)
    {
        _gameData = gameData;
    }
    
    /// <summary>
    /// Returns all data centers supported by the API.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Available data centers")]
    [Route("data-centers")]
    [ProducesResponseType(typeof(IEnumerable<DataCenter>), 200)]
    public IEnumerable<DataCenter> GetV1()
    {
        return GetV3();
    }
    
    /// <summary>
    /// Returns all data centers supported by the API.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Available data centers")]
    [Route("v{version:apiVersion}/data-centers")]
    [ProducesResponseType(typeof(IEnumerable<DataCenter>), 200)]
    public IEnumerable<DataCenter> GetV2()
    {
        return GetV3();
    }
    
    /// <summary>
    /// Returns all data centers supported by the API.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("3")]
    [ApiTag("Available data centers")]
    [Route("v{version:apiVersion}/game/data-centers")]
    [ProducesResponseType(typeof(IEnumerable<DataCenter>), 200)]
    public IEnumerable<DataCenter> GetV3()
    {
        return _gameData.DataCenters().Select(dc => new DataCenter
        {
            Name = dc.Name,
            Region = dc.Region,
            Worlds = dc.WorldIds,
        });
    }
}