using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Universalis.Application.Realtime;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Swagger;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V2;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
#if !DEBUG
[ApiExplorerSettings(IgnoreApi = true)]
#endif
public class WebSocketDevController : ControllerBase
{
    private readonly IGameDataProvider _gameData;
    private readonly ISocketProcessor _socketProcessor;
    private readonly IWebHostEnvironment _env;

    public WebSocketDevController(ISocketProcessor socketProcessor, IGameDataProvider gameData, IWebHostEnvironment env)
    {
        _gameData = gameData;
        _socketProcessor = socketProcessor;
        _env = env;
    }

    /// <summary>
    /// Connect to the WebSocket dev endpoint of the API. Requires a valid WebSocket client.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("ws-dev")]
    [ApiTag("WebSocket (Development)")]
    public Task Get(CancellationToken cancellationToken = default)
    {
        return GetV2(cancellationToken);
    }

    /// <summary>
    /// Connect to the WebSocket dev endpoint of the API. Requires a valid WebSocket client.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/ws-dev")]
    [ApiTag("WebSocket (Development)")]
    public async Task GetV2(CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            await Task.WhenAny(
                WebSocketHandler.Connect(HttpContext, _socketProcessor, cancellationToken),
                RunPublishLoop(cancellationToken));
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task RunPublishLoop(CancellationToken cancellationToken = default)
    {
        var items = _gameData.MarketableItemIds().ToList();
        var worlds = _gameData.AvailableWorldIds().ToList();
        var random = new Random();

        while (!cancellationToken.IsCancellationRequested)
        {
            _socketProcessor.Publish(new ItemUpdate
            {
                ItemId = items[random.Next(0, items.Count - 1)],
                WorldId = worlds[random.Next(0, worlds.Count - 1)],
            });

            await Task.Delay(TimeSpan.FromSeconds(1) / 70, cancellationToken);
        }
    }
}