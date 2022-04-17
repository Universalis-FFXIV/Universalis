using System.Text.Json.Serialization;

namespace Universalis.Application.Realtime.Message;

public class ItemUpdate : SocketMessage
{
    [JsonPropertyName("item")]
    public uint ItemId { get; init; }

    [JsonPropertyName("world")]
    public uint WorldId { get; init; }

    public ItemUpdate() : base(MessageKind.ItemUpdate)
    {
    }
}