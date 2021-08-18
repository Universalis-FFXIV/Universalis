using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema
{
    public class Listing
    {
        [JsonPropertyName("listingID")]
        public string ListingId { get; set; }
        
        [JsonPropertyName("hq")]
        public string Hq { get; set; }
        
        [JsonPropertyName("pricePerUnit")]
        public uint PricePerUnit { get; set; }
        
        [JsonPropertyName("quantity")]
        public uint Quantity { get; set; }
        
        [JsonPropertyName("retainerName")]
        public string RetainerName { get; set; }
        
        [JsonPropertyName("retainerID")]
        public string RetainerId { get; set; }

        [JsonPropertyName("retainerCity")]
        public int RetainerCityId { get; set; }

        [JsonPropertyName("creatorName")]
        public string CreatorName { get; set; }
        
        [JsonPropertyName("onMannequin")]
        public string OnMannequin { get; set; }
        
        [JsonPropertyName("sellerID")]
        public string SellerId { get; set; }
        
        [JsonPropertyName("creatorID")]
        public string CreatorId { get; set; }
        
        [JsonPropertyName("stainID")]
        public uint DyeId { get; set; }

        [JsonPropertyName("lastReviewTime")]
        public double LastReviewTimeUnixSeconds { get; set; }
        
        [JsonPropertyName("materia")]
        public List<Materia> Materia { get; set; }
    }
}