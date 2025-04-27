using System;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime;

public interface ISocketClient : IDisposable
{
    Action OnClose { get; set; }

    bool Running { get; }

    void Push(SocketMessage message);

    /// <summary>
    /// Runs the WebSocket loop.
    /// </summary>
    Task RunSocket(CancellationToken cancellationToken = default);
}