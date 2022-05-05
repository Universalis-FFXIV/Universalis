using MongoDB.Bson.Serialization.Attributes;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Tests.Mocks.Realtime.Messages;

public class MockMessage : SocketMessage
{
    [BsonElement("value")]
    public uint Value { get; init; }

    public MockMessage(params string[] channels) : base(channels)
    {
    }
}