using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Realtime.Messages;

public class SalesAdd : SocketMessage
{
    [BsonElement("item")]
    public int ItemId { get; init; }

    [BsonElement("world")]
    public int WorldId { get; init; }

    [BsonElement("sales")]
    public IList<SaleView> Sales { get; init; }

    [BsonIgnore]
    private Dictionary<string, string>? _filterValues;

    public SalesAdd() : base("sales", "add")
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