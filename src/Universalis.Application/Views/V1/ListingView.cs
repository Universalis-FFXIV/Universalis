using System.Collections.Generic;
using System.Text.Json.Serialization;
using Universalis.Application.Common;

namespace Universalis.Application.Views.V1;
/*
 * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
 * Please do not edit the field order unless it is unavoidable.
 */

public class ListingView : IPriceable
{
    /// <summary>
    /// The time that this listing was posted, in seconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("lastReviewTime")]
    public long LastReviewTimeUnixSeconds { get; init; }

    /// <summary>
    /// The price per unit sold.
    /// </summary>
    [JsonPropertyName("pricePerUnit")]
    public uint PricePerUnit { get; set; }

    /// <summary>
    /// The stack size sold.
    /// </summary>
    [JsonPropertyName("quantity")]
    public uint Quantity { get; init; }

    /// <summary>
    /// The ID of the dye on this item.
    /// </summary>
    [JsonPropertyName("stainID")]
    public uint DyeId { get; init; }

    /// <summary>
    /// The world name, if applicable.
    /// </summary>
    [JsonPropertyName("worldName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string WorldName { get; set; }

    /// <summary>
    /// The world ID, if applicable.
    /// </summary>
    [JsonPropertyName("worldID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? WorldId { get; set; }

    /// <summary>
    /// The creator's character name.
    /// </summary>
    [JsonPropertyName("creatorName")]
    public string CreatorName { get; init; }

    /// <summary>
    /// A SHA256 hash of the creator's ID.
    /// </summary>
    [JsonPropertyName("creatorID")]
    public string CreatorIdHash { get; set; }

    /// <summary>
    /// Whether or not the item is high-quality.
    /// </summary>
    [JsonPropertyName("hq")]
    public bool Hq { get; init; }

    /// <summary>
    /// Whether or not the item is crafted.
    /// </summary>
    [JsonPropertyName("isCrafted")]
    public bool IsCrafted { get; init; }

    /// <summary>
    /// A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null.
    /// </summary>
    [JsonPropertyName("listingID")]
    public string ListingIdHash { get; set; }

    /// <summary>
    /// The materia on this item.
    /// </summary>
    [JsonPropertyName("materia")]
    public List<MateriaView> Materia { get; init; } = new();

    /// <summary>
    /// Whether or not the item is being sold on a mannequin.
    /// </summary>
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
    [JsonPropertyName("retainerCity")]
    public int RetainerCityId { get; init; }

    /// <summary>
    /// A SHA256 hash of the retainer's ID.
    /// </summary>
    [JsonPropertyName("retainerID")]
    public string RetainerIdHash { get; set; }

    /// <summary>
    /// The retainer's name.
    /// </summary>
    [JsonPropertyName("retainerName")]
    public string RetainerName { get; init; }

    /// <summary>
    /// A SHA256 hash of the seller's ID.
    /// </summary>
    [JsonPropertyName("sellerID")]
    public string SellerIdHash { get; set; }

    /// <summary>
    /// The total price.
    /// </summary>
    [JsonPropertyName("total")]
    public uint Total { get; init; }
}