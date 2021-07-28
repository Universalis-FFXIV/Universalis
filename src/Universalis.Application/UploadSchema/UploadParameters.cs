using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.UploadSchema
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

        [JsonProperty("entries")]
        public IList<Sale> Sales { get; set; }

        [JsonProperty("contentID")]
        public string ContentId { get; set; }

        [JsonProperty("characterName")]
        public string CharacterName { get; set; }
    }
}