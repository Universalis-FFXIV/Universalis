﻿using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime;
using Universalis.Application.Realtime.Message;

namespace Universalis.Application.Tests.Mocks.Realtime;

public class MockSocketProcessor : ISocketProcessor
{
    public void BroadcastUpdate(SocketMessage message)
    {
    }

    public void AddSocket(WebSocket ws, TaskCompletionSource<object> cs, CancellationToken cancellationToken = default)
    {
    }
}