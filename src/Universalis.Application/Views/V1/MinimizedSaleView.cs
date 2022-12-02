using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1;
/*
 * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
 * Please do not edit the field order unless it is unavoidable.
 */

public class MinimizedSaleView
{
    /// <summary>
    /// Whether or not the item was high-quality.
    /// </summary>
    [JsonPropertyName("hq")]
    public bool Hq { get; init; }

    /// <summary>
    /// The price per unit sold.
    /// </summary>
    [JsonPropertyName("pricePerUnit")]
    public int PricePerUnit { get; init; }

    /// <summary>
    /// The stack size sold.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }
    
    /// <summary>
    /// The buyer's character name. This may be null.
    /// </summary>
    [JsonPropertyName("buyerName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string BuyerName { get; init; }
    
    /// <summary>
    /// Whether or not this was purchased from a mannequin. This may be null.
    /// </summary>
    [JsonPropertyName("onMannequin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OnMannequin { get; init; }

    /// <summary>
    /// The sale time, in seconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long TimestampUnixSeconds { get; init; }

    /// <summary>
    /// The world name, if applicable.
    /// </summary>
    [JsonPropertyName("worldName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string WorldName { get; init; }

    /// <summary>
    /// The world ID, if applicable.
    /// </summary>
    [JsonPropertyName("worldID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WorldId { get; init; }
}