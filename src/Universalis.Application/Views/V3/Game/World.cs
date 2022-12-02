using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Game;

public class World
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
    
    [JsonPropertyName("name")]
    public string Name { get; init; }
}