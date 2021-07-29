using Newtonsoft.Json;

namespace Universalis.Application.Uploads.Schema
{
    public class MarketTaxRates
    {
        [JsonProperty("limsaLominsa")]
        public byte LimsaLominsa { get; set; }

        [JsonProperty("gridania")]
        public byte Gridania { get; set; }

        [JsonProperty("uldah")]
        public byte Uldah { get; set; }

        [JsonProperty("ishgard")]
        public byte Ishgard { get; set; }

        [JsonProperty("kugane")]
        public byte Kugane { get; set; }

        [JsonProperty("crystarium")]
        public byte Crystarium { get; set; }
    }
}