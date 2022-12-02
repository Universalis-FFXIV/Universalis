using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Game;

public class DataCenter
{
    [JsonPropertyName("name")]
    public string Name { get; init; }
    
    [JsonPropertyName("region")]
    public string Region { get; init; }
    
    [JsonPropertyName("worlds")]
    public int[] Worlds { get; init; }
}