using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Uploads.Schema
{
    public class Listing
    {
        [JsonProperty("listingID")]
        public string ListingId { get; set; }
        
        [JsonProperty("hq")]
        public string Hq { get; set; }
        
        [JsonProperty("pricePerUnit")]
        public uint PricePerUnit { get; set; }
        
        [JsonProperty("quantity")]
        public uint Quantity { get; set; }
        
        [JsonProperty("retainerName")]
        public string RetainerName { get; set; }
        
        [JsonProperty("retainerID")]
        public string RetainerId { get; set; }

        [JsonProperty("retainerCity")]
        public byte RetainerCityId { get; set; }

        [JsonProperty("creatorName")]
        public string CreatorName { get; set; }
        
        [JsonProperty("onMannequin")]
        public string OnMannequin { get; set; }
        
        [JsonProperty("sellerID")]
        public string SellerId { get; set; }
        
        [JsonProperty("creatorID")]
        public string CreatorId { get; set; }
        
        [JsonProperty("stainID")]
        public uint DyeId { get; set; }

        [JsonProperty("lastReviewTime")]
        public uint LastReviewTimeUnixSeconds { get; set; }
        
        [JsonProperty("materia")]
        public List<Materia> Materia { get; set; }
    }
}