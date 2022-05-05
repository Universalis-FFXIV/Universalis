using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Realtime.Messages;

public abstract class SocketMessage
{
    [BsonElement("event")]
    public string Event => string.Join('/', ChannelsInternal);

    [BsonIgnore]
    public string[] ChannelsInternal { get; }

    protected SocketMessage(params string[] channels)
    {
        ChannelsInternal = channels;
    }
}