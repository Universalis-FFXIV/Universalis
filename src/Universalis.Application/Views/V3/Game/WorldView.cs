using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Game;

public class WorldView
{
    [JsonPropertyName("id")]
    public uint Id { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; }
}