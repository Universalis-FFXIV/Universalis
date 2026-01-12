using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Realtime.Messages;

public class ItemUpdate : SocketMessage
{
    [BsonElement("item")]
    public int ItemId { get; init; }

    [BsonElement("world")]
    public int WorldId { get; init; }

    [BsonIgnore]
    private Dictionary<string, string>? _filterValues;

    public ItemUpdate() : base("item", "update")
    {
    }

    public override IReadOnlyDictionary<string, string> GetFilterValues()
    {
        return _filterValues ??= new Dictionary<string, string>
        {
            ["item"] = ItemId.ToString(),
            ["world"] = WorldId.ToString(),
        };
    }
}