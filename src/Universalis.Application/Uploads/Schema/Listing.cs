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
    public object Hq { get; set; }
        
    [JsonPropertyName("pricePerUnit")]
    public uint PricePerUnit { get; set; }
        
    [JsonPropertyName("quantity")]
    public uint Quantity { get; set; }
        
    [JsonPropertyName("retainerName")]
    public string RetainerName { get; set; }
        
    [JsonPropertyName("retainerID")]
    public object RetainerIdInternal { get; set; }

    [JsonIgnore]
    public string RetainerId
    {
        get => Util.ParseUnusualId(RetainerIdInternal);
        set => RetainerIdInternal = value;
    }

    [JsonPropertyName("retainerCity")]
    public int RetainerCityId { get; set; }

    [JsonPropertyName("creatorName")]
    public string CreatorName { get; set; }
        
    [JsonPropertyName("onMannequin")]
    public object OnMannequin { get; set; }
        
    [JsonPropertyName("sellerID")]
    public object SellerIdInternal { get; set; }

    [JsonIgnore]
    public string SellerId
    {
        get => Util.ParseUnusualId(SellerIdInternal);
        set => SellerIdInternal = value;
    }

    [JsonPropertyName("creatorID")]
    public object CreatorIdInternal { get; set; }

    [JsonIgnore]
    public string CreatorId
    {
        get => Util.ParseUnusualId(CreatorIdInternal);
        set => CreatorIdInternal = value;
    }

    [JsonPropertyName("stainID")]
    public uint DyeId { get; set; }

    [JsonPropertyName("lastReviewTime")]
    public double LastReviewTimeUnixSeconds { get; set; }
        
    [JsonPropertyName("materia")]
    public List<Materia> Materia { get; set; }
}