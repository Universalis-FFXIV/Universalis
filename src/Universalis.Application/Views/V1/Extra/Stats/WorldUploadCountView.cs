using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1.Extra.Stats;

public class WorldUploadCountView
{
    /// <summary>
    /// The number of times an upload has occurred on this world.
    /// </summary>
    [JsonPropertyName("count")]
    public double Count { get; init; }

    /// <summary>
    /// The proportion of uploads on this world to the total number of uploads.
    /// </summary>
    [JsonPropertyName("proportion")]
    public double Proportion { get; init; }
}