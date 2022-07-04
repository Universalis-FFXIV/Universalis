using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Market;

public class Creator
{
    /// <summary>
    /// A SHA256 hash of the creator's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string IdHash { get; init; }
    
    /// <summary>
    /// The creator's character name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; }
}