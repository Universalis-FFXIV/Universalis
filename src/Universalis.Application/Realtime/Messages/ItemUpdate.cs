using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Realtime.Messages;

public class ItemUpdate : SocketMessage
{
    [BsonElement("item")]
    public uint ItemId { get; init; }

    [BsonElement("world")]
    public uint WorldId { get; init; }

    public ItemUpdate() : base("item", "update")
    {
    }
}