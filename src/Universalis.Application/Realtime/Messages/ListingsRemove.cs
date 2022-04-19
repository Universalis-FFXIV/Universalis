using System.Collections.Generic;
using System.Text.Json.Serialization;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Realtime.Messages;

public class ListingsRemove : SocketMessage
{
    [JsonPropertyName("item")]
    public uint ItemId { get; init; }

    [JsonPropertyName("world")]
    public uint WorldId { get; init; }

    [JsonPropertyName("removed")]
    public int DroppedListings { get; init; }

    [JsonPropertyName("kept")]
    public int KeptListings { get; init; }

    public ListingsRemove() : base("listings", "remove")
    {
    }
}