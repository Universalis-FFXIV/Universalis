using System.Text.Json.Serialization;

namespace Universalis.Application.Realtime.Message;

public class SocketMessage
{
    [JsonPropertyName("event")]
    public string Event { get; }

    protected SocketMessage(MessageKind kind)
    {
        Event = kind.ToEventName();
    }
}