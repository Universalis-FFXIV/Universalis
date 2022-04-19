using System.Collections.Generic;
using System.Text.Json.Serialization;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Realtime.Messages;

public class SalesAdd : SocketMessage
{
    [JsonPropertyName("item")]
    public uint ItemId { get; init; }

    [JsonPropertyName("world")]
    public uint WorldId { get; init; }

    [JsonPropertyName("sales")]
    public IList<SaleView> Sales { get; init; }

    public SalesAdd() : base("sales", "add")
    {
    }
}