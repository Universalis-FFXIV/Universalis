using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema;

public class MarketTaxRates
{
    [JsonPropertyName("limsaLominsa")]
    public int? LimsaLominsa { get; set; }

    [JsonPropertyName("gridania")]
    public int? Gridania { get; set; }

    [JsonPropertyName("uldah")]
    public int? Uldah { get; set; }

    [JsonPropertyName("ishgard")]
    public int? Ishgard { get; set; }

    [JsonPropertyName("kugane")]
    public int? Kugane { get; set; }

    [JsonPropertyName("crystarium")]
    public int? Crystarium { get; set; }

    [JsonPropertyName("sharlayan")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OldSharlayan { get; set; }
}