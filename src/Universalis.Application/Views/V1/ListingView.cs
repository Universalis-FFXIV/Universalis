using System.Collections.Generic;
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

public class ListingView : PartiallySerializable, IPriceable, ICopyable
{
    /// <summary>
    /// The time that this listing was posted, in seconds since the UNIX epoch.
    /// </summary>
    [BsonElement("lastReviewTime")]
    [JsonPropertyName("lastReviewTime")]
    public long LastReviewTimeUnixSeconds { get; init; }

    /// <summary>
    /// The price per unit sold.
    /// </summary>
    [BsonElement("pricePerUnit")]
    [JsonPropertyName("pricePerUnit")]
    public uint PricePerUnit { get; set; }

    /// <summary>
    /// The stack size sold.
    /// </summary>
    [BsonElement("quantity")]
    [JsonPropertyName("quantity")]
    public uint Quantity { get; init; }

    /// <summary>
    /// The ID of the dye on this item.
    /// </summary>
    [BsonElement("stainID")]
    [JsonPropertyName("stainID")]
    public uint DyeId { get; init; }

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
    /// The creator's character name.
    /// </summary>
    [BsonElement("creatorName")]
    [JsonPropertyName("creatorName")]
    public string CreatorName { get; init; }

    /// <summary>
    /// A SHA256 hash of the creator's ID.
    /// </summary>
    [BsonElement("creatorID")]
    [JsonPropertyName("creatorID")]
    public string CreatorIdHash { get; set; }

    /// <summary>
    /// Whether or not the item is high-quality.
    /// </summary>
    [BsonElement("hq")]
    [JsonPropertyName("hq")]
    public bool Hq { get; init; }

    /// <summary>
    /// Whether or not the item is crafted.
    /// </summary>
    [BsonElement("isCrafted")]
    [JsonPropertyName("isCrafted")]
    public bool IsCrafted { get; init; }

    /// <summary>
    /// A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null.
    /// </summary>
    [BsonElement("listingID")]
    [JsonPropertyName("listingID")]
    public string ListingIdHash { get; set; }

    /// <summary>
    /// The materia on this item.
    /// </summary>
    [BsonElement("materia")]
    [JsonPropertyName("materia")]
    public List<MateriaView> Materia { get; init; } = new();

    /// <summary>
    /// Whether or not the item is being sold on a mannequin.
    /// </summary>
    [BsonElement("onMannequin")]
    [JsonPropertyName("onMannequin")]
    public bool OnMannequin { get; init; }

    /// <summary>
    /// The city ID of the retainer.
    /// Limsa Lominsa = 1
    /// Gridania = 2
    /// Ul'dah = 3
    /// Ishgard = 4
    /// Kugane = 7
    /// Crystarium = 10
    /// </summary>
    [BsonElement("retainerCity")]
    [JsonPropertyName("retainerCity")]
    public int RetainerCityId { get; init; }

    /// <summary>
    /// A SHA256 hash of the retainer's ID.
    /// </summary>
    [BsonElement("retainerID")]
    [JsonPropertyName("retainerID")]
    public string RetainerIdHash { get; set; }

    /// <summary>
    /// The retainer's name.
    /// </summary>
    [BsonElement("retainerName")]
    [JsonPropertyName("retainerName")]
    public string RetainerName { get; init; }

    /// <summary>
    /// A SHA256 hash of the seller's ID.
    /// </summary>
    [BsonElement("sellerID")]
    [JsonPropertyName("sellerID")]
    public string SellerIdHash { get; set; }

    /// <summary>
    /// The total price.
    /// </summary>
    [BsonElement("total")]
    [JsonPropertyName("total")]
    public uint Total { get; set; }

    public ICopyable Clone()
    {
        return (ListingView)MemberwiseClone();
    }
}