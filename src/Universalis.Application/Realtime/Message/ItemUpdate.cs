using System.Text.Json.Serialization;

namespace Universalis.Application.Realtime.Message;

public class ItemUpdate : SocketMessage
{
    [JsonPropertyName("item")]
    public uint ItemId { get; set; }

    public ItemUpdate() : base(MessageKind.ItemUpdate)
    {
    }
}