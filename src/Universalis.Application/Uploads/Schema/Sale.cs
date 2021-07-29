using Newtonsoft.Json;

namespace Universalis.Application.Uploads.Schema
{
    public class Sale
    {
        [JsonProperty("hq")]
        public string Hq { get; set; }

        [JsonProperty("pricePerUnit")]
        public uint PricePerUnit { get; set; }
        
        [JsonProperty("quantity")]
        public uint Quantity { get; set; }
        
        [JsonProperty("buyerName")]
        public string BuyerName { get; set; }
        
        [JsonProperty("onMannequin")]
        public string OnMannequin { get; set; }
        
        [JsonProperty("sellerID")]
        public string SellerId { get; set; }
        
        [JsonProperty("buyerID")]
        public string BuyerId { get; set; }
        
        [JsonProperty("timestamp")]
        public uint TimestampUnixSeconds { get; set; }
    }
}