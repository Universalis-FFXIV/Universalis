using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universalis.Application.Uploads.Schema
{
    public class UploadParameters
    {
        [JsonProperty("uploaderID")]
        public string UploaderId { get; set; }

        [JsonProperty("worldID")]
        public uint? WorldId { get; set; }

        [JsonProperty("itemID")]
        public uint? ItemId { get; set; }

        [JsonProperty("marketTaxRates")]
        public MarketTaxRates TaxRates { get; set; }

        [JsonProperty("listings")]
        public IList<Listing> Listings { get; set; }

        [JsonProperty("entries")]
        public IList<Sale> Sales { get; set; }

        [JsonProperty("contentID")]
        public string ContentId { get; set; }

        [JsonProperty("characterName")]
        public string CharacterName { get; set; }
    }
}