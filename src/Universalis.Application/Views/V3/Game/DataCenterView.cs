using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Game;

public class DataCenterView
{
    [JsonPropertyName("name")]
    public string Name { get; init; }
    
    [JsonPropertyName("worlds")]
    public uint[] Worlds { get; init; }
}