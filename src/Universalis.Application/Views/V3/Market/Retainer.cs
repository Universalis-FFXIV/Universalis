using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Market;

public class Retainer
{
    /// <summary>
    /// A SHA256 hash of the retainer's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string IdHash { get; init; }
    
    /// <summary>
    /// The retainer's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; }
    
    /// <summary>
    /// The city ID of the retainer.
    /// Limsa Lominsa = 1
    /// Gridania = 2
    /// Ul'dah = 3
    /// Ishgard = 4
    /// Kugane = 7
    /// Crystarium = 10
    /// </summary>
    [JsonPropertyName("city")]
    public uint City { get; init; }
}