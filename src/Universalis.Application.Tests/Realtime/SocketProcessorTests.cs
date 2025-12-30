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
using Universalis.Application.Tests.Mocks.Realtime.Messages;
using Universalis.Common.Collections;
using Xunit;

namespace Universalis.Application.Tests.Realtime;

public class SocketProcessorTests
{
    [Fact]
    public void Publish_SetsCachedSerializedBytesOnMessage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SocketProcessor>>();
        var socketProcessor = new SocketProcessor(loggerMock.Object);
        var message = new MockMessage("test", "channel") { Value = 42 };

        // Act
        Assert.Null(message.CachedSerializedBytes); // Verify it starts null
        socketProcessor.Publish(message);

        // Assert - CachedSerializedBytes should now be set
        Assert.NotNull(message.CachedSerializedBytes);
        Assert.True(message.CachedSerializedBytes.Length > 0);
    }

    [Fact]
    public void Publish_ContinuesWhenOneClientThrows()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SocketProcessor>>();
        var socketProcessor = new SocketProcessor(loggerMock.Object);

        var goodClient1 = new Mock<ISocketClient>();
        var badClient = new Mock<ISocketClient>();
        var goodClient2 = new Mock<ISocketClient>();

        // Configure badClient to throw exception on Push
        badClient.Setup(c => c.Push(It.IsAny<SocketMessage>()))
            .Throws(new InvalidOperationException("Simulated client error"));

        var message = new MockMessage("test") { Value = 42 };

        // Add clients to the processor
        var connectionsField = typeof(SocketProcessor).GetField("_connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connections = new ConcurrentDictionary<Guid, ISocketClient>
        {
            [Guid.NewGuid()] = goodClient1.Object,
            [Guid.NewGuid()] = badClient.Object,
            [Guid.NewGuid()] = goodClient2.Object,
        };
        connectionsField.SetValue(socketProcessor, connections);

        // Act - should not throw despite badClient throwing
        socketProcessor.Publish(message);

        // Assert - good clients should still have received the message
        goodClient1.Verify(c => c.Push(message), Times.Once);
        goodClient2.Verify(c => c.Push(message), Times.Once);
        badClient.Verify(c => c.Push(message), Times.Once); // Was called but threw
    }

    [Fact]
    public void Publish_FuzzTest()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SocketProcessor>>();

        var socketProcessor = new SocketProcessor(loggerMock.Object);

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
        var socketProcessor = new SocketProcessor(loggerMock.Object);

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

        var socketProcessor = new SocketProcessor(loggerMock.Object);

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