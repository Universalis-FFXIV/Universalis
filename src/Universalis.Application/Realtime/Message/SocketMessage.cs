using System.Text.Json.Serialization;

namespace Universalis.Application.Realtime.Message;

public abstract class SocketMessage
{
    [JsonPropertyName("event")]
    public string Event { get; }

    protected SocketMessage(MessageKind kind)
    {
        Event = kind.ToEventName();
    }
}