using System.Text.Json.Serialization;

namespace Universalis.Application.Views
{
    public class TaxRatesView
    {
        /// <summary>
        /// The percent retainer tax in Limsa Lominsa.
        /// </summary>
        [JsonPropertyName("Limsa Lominsa")]
        public byte LimsaLominsa { get; init; }

        /// <summary>
        /// The percent retainer tax in Gridania.
        /// </summary>
        [JsonPropertyName("Gridania")]
        public byte Gridania { get; init; }

        /// <summary>
        /// The percent retainer tax in Ul'dah.
        /// </summary>
        [JsonPropertyName("Ul'dah")]
        public byte Uldah { get; init; }

        /// <summary>
        /// The percent retainer tax in Ishgard.
        /// </summary>
        [JsonPropertyName("Ishgard")]
        public byte Ishgard { get; init; }

        /// <summary>
        /// The percent retainer tax in Kugane.
        /// </summary>
        [JsonPropertyName("Kugane")]
        public byte Kugane { get; init; }

        /// <summary>
        /// The percent retainer tax in the Crystarium.
        /// </summary>
        [JsonPropertyName("Crystarium")]
        public byte Crystarium { get; init; }

        /// <summary>
        /// The percent retainer tax in Old Sharlayan.
        /// </summary>
        [JsonPropertyName("Old Sharlayan")]
        public byte OldSharlayan { get; init; }
    }
}