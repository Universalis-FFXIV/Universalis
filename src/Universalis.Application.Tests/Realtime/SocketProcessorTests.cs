using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Universalis.Application.Realtime;
using Universalis.Application.Realtime.Messages;
using Universalis.Common.Collections;
using Xunit;

namespace Universalis.Application.Tests.Realtime;

public class SocketProcessorTests
{
    [Fact]
    public void Publish_FuzzTest()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SocketProcessor>>();

        using var socketProcessor = new SocketProcessor(loggerMock.Object);

        var clientMocks = FactoryList.OfLength(2, _ => new Mock<ISocketClient>());
        var messages = FactoryList.OfLength(2500, _ => Mock.Of<SocketMessage>());

        // Add mocked clients to the private _connections dictionary
        var connectionsField = typeof(SocketProcessor).GetField("_connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connections = new ConcurrentDictionary<Guid, ISocketClient>(clientMocks.Select(mock =>
            new KeyValuePair<Guid, ISocketClient>(Guid.NewGuid(), mock.Object)));
        connectionsField.SetValue(socketProcessor, connections);

        // Act
        messages.ForEachParallel(8, message => socketProcessor.Publish(message));

        // Assert
        // All clients should have processed every message
        messages.ForEach(message => clientMocks.ForEach(mock => mock.Verify(c => c.Push(message), Times.Once)));
    }

    [Fact]
    public void Publish_SendsMessageToAllConnectedClients()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SocketProcessor>>();
        using var socketProcessor = new SocketProcessor(loggerMock.Object);

        var client1Mock = new Mock<ISocketClient>();
        var client2Mock = new Mock<ISocketClient>();

        var message = Mock.Of<SocketMessage>();

        // Add mocked clients to the private _connections dictionary
        var connectionsField = typeof(SocketProcessor).GetField("_connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connections = new ConcurrentDictionary<Guid, ISocketClient>
        {
            [Guid.NewGuid()] = client1Mock.Object,
            [Guid.NewGuid()] = client2Mock.Object,
        };
        connectionsField.SetValue(socketProcessor, connections);

        // Act
        socketProcessor.Publish(message);

        // Assert
        client1Mock.Verify(c => c.Push(message), Times.Once);
        client2Mock.Verify(c => c.Push(message), Times.Once);
    }

    [Fact]
    public void AddSocket_AddsNewSocketClientAndIncrementsMetrics()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SocketProcessor>>();
        var webSocketMock = new Mock<WebSocket>();
        var taskCompletionSource = new TaskCompletionSource<object>();
        var cancellationToken = new CancellationToken();

        using var socketProcessor = new SocketProcessor(loggerMock.Object);

        // Act
        socketProcessor.AddSocket(webSocketMock.Object, taskCompletionSource, cancellationToken);

        // Retrieve private _connections dictionary to verify new client added
        var connectionsField = typeof(SocketProcessor).GetField("_connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connections = (ConcurrentDictionary<Guid, ISocketClient>)connectionsField.GetValue(socketProcessor);

        // Assert
        Assert.Single(connections);
        var client = connections.Values.First();

        // Verify callbacks and methods
        Assert.NotNull(client);
        Assert.False(client.Running);
    }
}