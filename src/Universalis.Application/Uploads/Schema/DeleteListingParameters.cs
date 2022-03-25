
using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema;

public class DeleteListingParameters
{
    [JsonPropertyName("retainerID")]
    public string RetainerId { get; set; }

    [JsonPropertyName("listingID")]
    public string ListingId { get; set; }

    [JsonPropertyName("quantity")]
    public uint Quantity { get; set; }

    [JsonPropertyName("pricePerUnit")]
    public uint PricePerUnit { get; set; }

    [JsonPropertyName("uploaderID")]
    public string UploaderId { get; set; }
}