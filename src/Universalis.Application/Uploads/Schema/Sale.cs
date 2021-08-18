using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema
{
    public class Sale
    {
        [JsonPropertyName("hq")]
        public string Hq { get; set; }

        [JsonPropertyName("pricePerUnit")]
        public uint PricePerUnit { get; set; }
        
        [JsonPropertyName("quantity")]
        public uint Quantity { get; set; }
        
        [JsonPropertyName("buyerName")]
        public string BuyerName { get; set; }
        
        [JsonPropertyName("onMannequin")]
        public string OnMannequin { get; set; }
        
        [JsonPropertyName("sellerID")]
        public string SellerId { get; set; }
        
        [JsonPropertyName("buyerID")]
        public string BuyerId { get; set; }
        
        [JsonPropertyName("timestamp")]
        public double TimestampUnixSeconds { get; set; }
    }
}