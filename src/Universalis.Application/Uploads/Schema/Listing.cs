using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema;

public class Listing
{
    [JsonPropertyName("listingID")]
    public object ListingIdInternal { get; set; }

    [JsonIgnore]
    public string ListingId
    {
        get => Util.ParseUnusualId(ListingIdInternal);
        set => ListingIdInternal = value;
    }

    [JsonPropertyName("hq")]
    public object Hq { get; init; }
        
    [JsonPropertyName("pricePerUnit")]
    public uint? PricePerUnit { get; init; }
        
    [JsonPropertyName("quantity")]
    public uint? Quantity { get; set; }
        
    [JsonPropertyName("retainerName")]
    public string RetainerName { get; init; }
        
    [JsonPropertyName("retainerID")]
    public object RetainerIdInternal { get; init; }

    [JsonIgnore]
    public string RetainerId
    {
        get => Util.ParseUnusualId(RetainerIdInternal);
        init => RetainerIdInternal = value;
    }

    [JsonPropertyName("retainerCity")]
    public int? RetainerCityId { get; init; }

    [JsonPropertyName("creatorName")]
    public string CreatorName { get; init; }
        
    [JsonPropertyName("onMannequin")]
    public object OnMannequin { get; init; }
        
    [JsonPropertyName("sellerID")]
    public object SellerIdInternal { get; init; }

    [JsonIgnore]
    public string SellerId
    {
        get => Util.ParseUnusualId(SellerIdInternal);
        init => SellerIdInternal = value;
    }

    [JsonPropertyName("creatorID")]
    public object CreatorIdInternal { get; init; }

    [JsonIgnore]
    public string CreatorId
    {
        get => Util.ParseUnusualId(CreatorIdInternal);
        init => CreatorIdInternal = value;
    }

    [JsonPropertyName("stainID")]
    public uint? DyeId { get; init; }

    [JsonPropertyName("lastReviewTime")]
    public long? LastReviewTimeUnixSeconds { get; init; }
        
    [JsonPropertyName("materia")]
    public List<Materia> Materia { get; init; }
}