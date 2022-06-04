using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1;

public class TaxRatesView
{
    /// <summary>
    /// The percent retainer tax in Limsa Lominsa.
    /// </summary>
    [JsonPropertyName("Limsa Lominsa")]
    public int LimsaLominsa { get; init; }

    /// <summary>
    /// The percent retainer tax in Gridania.
    /// </summary>
    [JsonPropertyName("Gridania")]
    public int Gridania { get; init; }

    /// <summary>
    /// The percent retainer tax in Ul'dah.
    /// </summary>
    [JsonPropertyName("Ul'dah")]
    public int Uldah { get; init; }

    /// <summary>
    /// The percent retainer tax in Ishgard.
    /// </summary>
    [JsonPropertyName("Ishgard")]
    public int Ishgard { get; init; }

    /// <summary>
    /// The percent retainer tax in Kugane.
    /// </summary>
    [JsonPropertyName("Kugane")]
    public int Kugane { get; init; }

    /// <summary>
    /// The percent retainer tax in the Crystarium.
    /// </summary>
    [JsonPropertyName("Crystarium")]
    public int Crystarium { get; init; }

    /// <summary>
    /// The percent retainer tax in Old Sharlayan.
    /// </summary>
    [JsonPropertyName("Old Sharlayan")]
    public int OldSharlayan { get; init; }
}