using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Universalis.Application.Realtime;

public interface ISocketProcessor
{
    void AddSocket(WebSocket ws, TaskCompletionSource<object> cs);
}