using Microsoft.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Tests.Mocks.Realtime.Messages;
using Xunit;

namespace Universalis.Application.Tests.Realtime.Messages;

public class SocketMessageTests
{
    private static readonly RecyclableMemoryStreamManager Pool = new();

    [Fact]
    public void GetSerializedBytes_ReturnsCachedBytesWhenAvailable()
    {
        // Arrange
        var message = new MockMessage("test") { Value = 42 };
        var cachedBytes = new byte[] { 1, 2, 3, 4, 5 };
        message.CachedSerializedBytes = cachedBytes;

        // Act
        var result = message.GetSerializedBytes(Pool);

        // Assert - verify it returns the exact cached bytes
        Assert.Equal(cachedBytes, result.ToArray());
    }

    [Fact]
    public void GetSerializedBytes_SerializesWhenNoCachedBytes()
    {
        // Arrange
        var message = new MockMessage("test") { Value = 42 };

        // Act
        var result = message.GetSerializedBytes(Pool);

        // Assert
        Assert.False(result.IsEmpty);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GetSerializedBytes_ProducesValidBson()
    {
        // Arrange
        var message = new MockMessage("test", "channel") { Value = 123 };

        // Act
        var bytes = message.GetSerializedBytes(Pool);
        var deserialized = BsonSerializer.Deserialize<BsonDocument>(bytes.ToArray());

        // Assert
        Assert.Equal("test/channel", deserialized["event"].AsString);
        Assert.Equal(123, deserialized["value"].AsInt32);
    }

    [Fact]
    public void GetSerializedBytes_CachedAndFreshProduceSameOutput()
    {
        // Arrange
        var message1 = new MockMessage("test", "channel") { Value = 999 };
        var message2 = new MockMessage("test", "channel") { Value = 999 };

        // Act - serialize message1 fresh, then cache its bytes on message2
        var freshBytes = message1.GetSerializedBytes(Pool).ToArray();
        message2.CachedSerializedBytes = freshBytes;
        var cachedBytes = message2.GetSerializedBytes(Pool).ToArray();

        // Assert - both should be identical
        Assert.Equal(freshBytes, cachedBytes);
    }

    [Fact]
    public void GetSerializedBytes_WorksForSubscribeFailure()
    {
        // Arrange - SubscribeFailure is sent per-client without caching
        var message = new SubscribeFailure("test error");

        // Act - should use fallback serialization path
        var bytes = message.GetSerializedBytes(Pool);
        var deserialized = BsonSerializer.Deserialize<BsonDocument>(bytes.ToArray());

        // Assert
        Assert.Equal("subscribe/error", deserialized["event"].AsString);
        Assert.Equal("test error", deserialized["reason"].AsString);
    }
}
