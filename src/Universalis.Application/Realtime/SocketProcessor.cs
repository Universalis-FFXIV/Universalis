using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.Application.Realtime;

public class SocketProcessor : ISocketProcessor
{
    public async Task AddSocket(WebSocket ws, TaskCompletionSource<object> cs)
    {
        await ws.CloseAsync(
            WebSocketCloseStatus.Empty,
            "",
            CancellationToken.None);

        cs.TrySetResult(true);
    }
}