using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class TaxRatesView
    {
        [JsonProperty("Limsa Lominsa")]
        public byte LimsaLominsa { get; set; }

        [JsonProperty("Gridania")]
        public byte Gridania { get; set; }

        [JsonProperty("Ul'dah")]
        public byte Uldah { get; set; }

        [JsonProperty("Ishgard")]
        public byte Ishgard { get; set; }

        [JsonProperty("Kugane")]
        public byte Kugane { get; set; }

        [JsonProperty("Crystarium")]
        public byte Crystarium { get; set; }
    }
}