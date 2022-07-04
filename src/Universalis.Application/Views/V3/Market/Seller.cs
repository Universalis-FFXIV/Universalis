using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Market;

public class Seller
{
    /// <summary>
    /// A SHA256 hash of the seller's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string IdHash { get; set; }
}