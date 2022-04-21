using System.Text.Json.Serialization;

namespace Universalis.Application.Realtime.Messages;

public abstract class SocketMessage
{
    [JsonPropertyName("event")]
    public string Event => string.Join('/', ChannelsInternal);

    [JsonIgnore]
    public string[] ChannelsInternal { get; }

    protected SocketMessage(params string[] channels)
    {
        ChannelsInternal = channels;
    }
}