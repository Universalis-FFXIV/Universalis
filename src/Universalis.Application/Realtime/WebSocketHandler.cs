using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Universalis.Application.Realtime;

public static class WebSocketHandler
{
    public static async Task Connect(HttpContext ctx, ISocketProcessor socketProcessor, CancellationToken cancellationToken = default)
    {
        using var webSocket = await ctx.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext());
        var socketFinished = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        socketProcessor.AddSocket(webSocket, socketFinished, cancellationToken);

        await socketFinished.Task;
    }
}