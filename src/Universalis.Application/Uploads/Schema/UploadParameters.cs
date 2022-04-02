using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema;

public class UploadParameters
{
    [JsonPropertyName("uploaderID")]
    public object? UploaderIdInternal { get; set; }

    [JsonIgnore]
    public string? UploaderId
    {
        get => Util.ParseUnusualId(UploaderIdInternal);
        set => UploaderIdInternal = value;
    }

    [JsonPropertyName("worldID")]
    public uint? WorldId { get; set; }

    [JsonPropertyName("itemID")]
    public uint? ItemId { get; set; }

    [JsonPropertyName("marketTaxRates")]
    public MarketTaxRates? TaxRates { get; set; }

    [JsonPropertyName("listings")]
    public IList<Listing>? Listings { get; set; }

    [JsonPropertyName("entries")]
    public IList<Sale>? Sales { get; set; }

    [JsonPropertyName("contentID")]
    public object? ContentIdInternal { get; set; }

    [JsonIgnore]
    public string? ContentId
    {
        get => Util.ParseUnusualId(ContentIdInternal);
        set => ContentIdInternal = value;
    }

    [JsonPropertyName("characterName")]
    public string? CharacterName { get; set; }
}