using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema;

public class Sale
{
    [JsonPropertyName("hq")]
    public object Hq { get; init; }

    [JsonPropertyName("pricePerUnit")]
    public uint? PricePerUnit { get; init; }
        
    [JsonPropertyName("quantity")]
    public uint? Quantity { get; set; }
        
    [JsonPropertyName("buyerName")]
    public string BuyerName { get; init; }
        
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

    [JsonPropertyName("buyerID")]
    public object BuyerIdInternal { get; set; }

    [JsonIgnore]
    public string BuyerId
    {
        get => Util.ParseUnusualId(BuyerIdInternal);
        set => BuyerIdInternal = value;
    }

    [JsonPropertyName("timestamp")]
    public long? TimestampUnixSeconds { get; init; }
}