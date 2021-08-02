using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class SaleView
    {
        [JsonProperty("hq")]
        public bool Hq { get; set; }

        [JsonProperty("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [JsonProperty("quantity")]
        public uint Quantity { get; set; }

        [JsonProperty("total")]
        public uint Total { get; set; }

        [JsonProperty("buyerName")]
        public string BuyerName { get; set; }

        [JsonProperty("timestamp")]
        public double TimestampUnixSeconds { get; set; }

        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }
    }
}