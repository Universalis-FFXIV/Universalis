using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema;

public class MarketTaxRates
{
    [JsonPropertyName("limsaLominsa")]
    public byte LimsaLominsa { get; set; }

    [JsonPropertyName("gridania")]
    public byte Gridania { get; set; }

    [JsonPropertyName("uldah")]
    public byte Uldah { get; set; }

    [JsonPropertyName("ishgard")]
    public byte Ishgard { get; set; }

    [JsonPropertyName("kugane")]
    public byte Kugane { get; set; }

    [JsonPropertyName("crystarium")]
    public byte Crystarium { get; set; }

    [JsonPropertyName("sharlayan")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? OldSharlayan { get; set; }
}