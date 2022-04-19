using System.Text.Json.Serialization;

namespace Universalis.Application.Realtime.Messages;

public abstract class SocketMessage
{
    [JsonPropertyName("event")]
    public string Event { get; }

    protected SocketMessage(params string[] channels)
    {
        Event = string.Join('/', channels);
    }
}