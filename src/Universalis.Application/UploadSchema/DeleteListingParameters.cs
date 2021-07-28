using Newtonsoft.Json;

namespace Universalis.Application.UploadSchema
{
    public class DeleteListingParameters
    {
        [JsonProperty("retainerID")]
        public string RetainerId { get; set; }

        [JsonProperty("listingID")]
        public string ListingId { get; set; }

        [JsonProperty("quantity")]
        public uint Quantity { get; set; }

        [JsonProperty("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [JsonProperty("uploaderID")]
        public string UploaderId { get; set; }
    }
}