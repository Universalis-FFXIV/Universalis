using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Universalis.Application.Caching;
using Universalis.Application.Common;
using Universalis.Common.Caching;

namespace Universalis.Application.Views.V1;
/*
 * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
 * Please do not edit the field order unless it is unavoidable.
 */

public class SaleView : IPriceable, ICopyable
{
    /// <summary>
    /// Whether or not the item was high-quality.
    /// </summary>
    [BsonElement("hq")]
    [JsonPropertyName("hq")]
    public bool Hq { get; init; }

    /// <summary>
    /// The price per unit sold.
    /// </summary>
    [BsonElement("pricePerUnit")]
    [JsonPropertyName("pricePerUnit")]
    public uint PricePerUnit { get; init; }

    /// <summary>
    /// The stack size sold.
    /// </summary>
    [BsonElement("quantity")]
    [JsonPropertyName("quantity")]
    public uint Quantity { get; init; }

    /// <summary>
    /// The sale time, in seconds since the UNIX epoch.
    /// </summary>
    [BsonElement("timestamp")]
    [JsonPropertyName("timestamp")]
    public long TimestampUnixSeconds { get; init; }
    
    /// <summary>
    /// Whether or not this was purchased from a mannequin. This may be null.
    /// </summary>
    [BsonElement("onMannequin")]
    [JsonPropertyName("onMannequin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OnMannequin { get; init; }

    /// <summary>
    /// The world name, if applicable.
    /// </summary>
    [BsonElement("worldName")]
    [JsonPropertyName("worldName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string WorldName { get; set; }

    /// <summary>
    /// The world ID, if applicable.
    /// </summary>
    [BsonElement("worldID")]
    [JsonPropertyName("worldID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? WorldId { get; set; }

    /// <summary>
    /// The buyer name.
    /// </summary>
    [BsonElement("buyerName")]
    [JsonPropertyName("buyerName")]
    public string BuyerName { get; init; }

    /// <summary>
    /// The total price.
    /// </summary>
    [BsonElement("total")]
    [JsonPropertyName("total")]
    public uint Total { get; init; }

    public ICopyable Clone()
    {
        return (SaleView)MemberwiseClone();
    }
}