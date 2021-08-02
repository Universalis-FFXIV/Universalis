using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universalis.Application.Views
{
    public class ListingView
    {
        [JsonProperty("listingID")]
        public string ListingId { get; set; }

        [JsonProperty("hq")]
        public bool Hq { get; set; }

        [JsonProperty("onMannequin")]
        public bool OnMannequin { get; set; }

        [JsonProperty("materia")]
        public List<MateriaView> Materia { get; set; } = new();

        [JsonProperty("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [JsonProperty("quantity")]
        public uint Quantity { get; set; }

        [JsonProperty("total")]
        public uint Total { get; set; }

        [JsonProperty("stainID")]
        public uint DyeId { get; set; }

        [JsonProperty("creatorID")]
        public string CreatorIdHash { get; set; }

        [JsonProperty("creatorName")]
        public string CreatorName { get; set; }

        [JsonProperty("isCrafted")]
        public bool IsCrafted { get; set; }

        [JsonProperty("lastReviewTime")]
        public double LastReviewTimeUnixSeconds { get; set; }

        [JsonProperty("retainerID")]
        public string RetainerId { get; set; }

        [JsonProperty("retainerName")]
        public string RetainerName { get; set; }

        [JsonProperty("retainerCity")]
        public int RetainerCityId { get; set; }

        [JsonProperty("sellerID")]
        public string SellerIdHash { get; set; }

        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }
    }
}