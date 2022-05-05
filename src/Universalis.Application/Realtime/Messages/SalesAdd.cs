using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Realtime.Messages;

public class SalesAdd : SocketMessage
{
    [BsonElement("item")]
    public uint ItemId { get; init; }

    [BsonElement("world")]
    public uint WorldId { get; init; }

    [BsonElement("sales")]
    public IList<SaleView> Sales { get; init; }

    public SalesAdd() : base("sales", "add")
    {
    }
}