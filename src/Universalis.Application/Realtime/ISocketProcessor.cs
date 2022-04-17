using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Message;

namespace Universalis.Application.Realtime;

public interface ISocketProcessor
{
    void BroadcastUpdate(SocketMessage message);

    void AddSocket(WebSocket ws, TaskCompletionSource<object> cs, CancellationToken cancellationToken = default);
}