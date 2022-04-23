using System.Text.Json.Serialization;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Tests.Mocks.Realtime.Messages;

public class MockMessage : SocketMessage
{
    [JsonPropertyName("value")]
    public uint Value { get; init; }

    public MockMessage(params string[] channels) : base(channels)
    {
    }
}