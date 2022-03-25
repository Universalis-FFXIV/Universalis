using System.Text.Json.Serialization;

namespace Universalis.Application.Views.Extra.Stats;

public class SourceUploadCountView
{
    /// <summary>
    /// The name of the client application.
    /// </summary>
    [JsonPropertyName("sourceName")]
    public string Name { get; init; }

    /// <summary>
    /// The number of uploads originating from the client application.
    /// </summary>
    [JsonPropertyName("uploadCount")]
    public double UploadCount { get; init; }
}