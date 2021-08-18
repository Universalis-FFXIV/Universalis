using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema
{
    public class UploadParameters
    {
        [JsonPropertyName("uploaderID")]
        public string UploaderId { get; set; }

        [JsonPropertyName("worldID")]
        public uint? WorldId { get; set; }

        [JsonPropertyName("itemID")]
        public uint? ItemId { get; set; }

        [JsonPropertyName("marketTaxRates")]
        public MarketTaxRates TaxRates { get; set; }

        [JsonPropertyName("listings")]
        public IList<Listing> Listings { get; set; }

        [JsonPropertyName("entries")]
        public IList<Sale> Sales { get; set; }

        [JsonPropertyName("contentID")]
        public string ContentId { get; set; }

        [JsonPropertyName("characterName")]
        public string CharacterName { get; set; }
    }
}