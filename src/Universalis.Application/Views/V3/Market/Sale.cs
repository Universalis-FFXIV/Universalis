using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Market;

public class Sale
{
    /// <summary>
    /// The sale world's ID.
    /// </summary>
    [JsonPropertyName("world")]
    public int World { get; init; }
    
    /// <summary>
    /// Whether or not the item was high-quality.
    /// </summary>
    [JsonPropertyName("hq")]
    public bool Hq { get; init; }

    /// <summary>
    /// The untaxed price per unit sold.
    /// </summary>
    [JsonPropertyName("price")]
    public int PricePerUnit { get; init; }

    /// <summary>
    /// The stack size sold. This may be null.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    /// <summary>
    /// The sale time, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("saleTime")]
    public long TimestampUnixMilliseconds { get; init; }
    
    /// <summary>
    /// Whether or not this was purchased from a mannequin. This may be null.
    /// </summary>
    [JsonPropertyName("onMannequin")]
    public bool? OnMannequin { get; init; }

    /// <summary>
    /// The buyer's name. This may be null.
    /// </summary>
    [JsonPropertyName("buyer")]
    public string BuyerName { get; init; }
}