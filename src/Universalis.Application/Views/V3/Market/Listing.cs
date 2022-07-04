using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Market;

public class Listing
{
    /// <summary>
    /// A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null.
    /// </summary>
    [JsonPropertyName("listingId")]
    public string ListingIdHash { get; set; }
    
    /// <summary>
    /// The listing world's ID.
    /// </summary>
    [JsonPropertyName("world")]
    public uint World { get; init; }
    
    /// <summary>
    /// The time that this listing was posted by its seller, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("reviewedAt")]
    public long LastReviewTimeUnixMilliseconds { get; init; }

    /// <summary>
    /// The price per unit sold. This is the result of the taxed total divided by the quantity, and so it
    /// will be a decimal value.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal PricePerUnit { get; init; }

    /// <summary>
    /// The stack size sold.
    /// </summary>
    [JsonPropertyName("quantity")]
    public uint Quantity { get; init; }
    
    /// <summary>
    /// The total price of the listing.
    /// </summary>
    [JsonPropertyName("total")]
    public uint Total { get; init; }

    /// <summary>
    /// Whether or not the item is high-quality.
    /// </summary>
    [JsonPropertyName("hq")]
    public bool Hq { get; init; }
    
    /// <summary>
    /// The ID of the dye color (from the Stain sheet) on this item.
    /// </summary>
    [JsonPropertyName("dye")]
    public uint DyeId { get; init; }

    /// <summary>
    /// The item's creator/crafter, if applicable. This may be null, for items with no creator.
    /// </summary>
    [JsonPropertyName("creator")]
    public Creator Creator { get; init; }

    /// <summary>
    /// The item IDs of the materia on this item.
    /// </summary>
    [JsonPropertyName("materia")]
    public IList<uint> Materia { get; init; }

    /// <summary>
    /// Whether or not the item is being sold on a mannequin.
    /// </summary>
    [JsonPropertyName("onMannequin")]
    public bool OnMannequin { get; init; }
    
    /// <summary>
    /// The retainer posting the listing.
    /// </summary>
    [JsonPropertyName("retainer")]
    public Retainer Retainer { get; init; }

    /// <summary>
    /// A SHA256 hash of the seller's ID.
    /// </summary>
    [JsonPropertyName("seller")]
    public Seller Seller { get; set; }
}