using System.Collections.Generic;
using System.Text.Json.Serialization;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Realtime.Messages;

public class ListingsAdd : SocketMessage
{
    [JsonPropertyName("item")]
    public uint ItemId { get; init; }

    [JsonPropertyName("world")]
    public uint WorldId { get; init; }

    [JsonPropertyName("listings")]
    public IList<ListingView> Listings { get; init; }

    public ListingsAdd() : base("listings", "add")
    {
    }
}