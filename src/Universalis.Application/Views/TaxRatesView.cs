using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class TaxRatesView
    {
        /// <summary>
        /// The percent retainer tax in Limsa Lominsa.
        /// </summary>
        [JsonProperty("Limsa Lominsa")]
        public byte LimsaLominsa { get; set; }

        /// <summary>
        /// The percent retainer tax in Gridania.
        /// </summary>
        [JsonProperty("Gridania")]
        public byte Gridania { get; set; }

        /// <summary>
        /// The percent retainer tax in Ul'dah.
        /// </summary>
        [JsonProperty("Ul'dah")]
        public byte Uldah { get; set; }

        /// <summary>
        /// The percent retainer tax in Ishgard.
        /// </summary>
        [JsonProperty("Ishgard")]
        public byte Ishgard { get; set; }

        /// <summary>
        /// The percent retainer tax in Kugane.
        /// </summary>
        [JsonProperty("Kugane")]
        public byte Kugane { get; set; }

        /// <summary>
        /// The percent retainer tax in the Crystarium.
        /// </summary>
        [JsonProperty("Crystarium")]
        public byte Crystarium { get; set; }
    }
}