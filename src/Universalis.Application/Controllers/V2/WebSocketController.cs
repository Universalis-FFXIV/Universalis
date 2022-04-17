using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime;
using Universalis.Application.Swagger;

namespace Universalis.Application.Controllers.V2;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
#if !DEBUG
[ApiExplorerSettings(IgnoreApi = true)]
#endif
public class WebSocketController : ControllerBase
{
    private readonly ISocketProcessor _socketProcessor;

    public WebSocketController(ISocketProcessor socketProcessor)
    {
        _socketProcessor = socketProcessor;
    }

    /// <summary>
    /// Connect to the WebSocket endpoint of the API. Requires a valid WebSocket client.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("ws")]
    [ApiTag("WebSocket")]
    public Task Get(CancellationToken cancellationToken = default)
    {
        return GetV2(cancellationToken);
    }

    /// <summary>
    /// Connect to the WebSocket endpoint of the API. Requires a valid WebSocket client.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/ws")]
    [ApiTag("WebSocket")]
    public async Task GetV2(CancellationToken cancellationToken = default)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync(
                new WebSocketAcceptContext { DangerousEnableCompression = true });
            var socketFinished = new TaskCompletionSource<object>();

            _socketProcessor.AddSocket(webSocket, socketFinished, cancellationToken);

            await socketFinished.Task;
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}