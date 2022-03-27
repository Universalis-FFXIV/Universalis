using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserReportView
{
    /// <summary>
    /// The report's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The timestamp of the report.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string TimestampMs { get; set; }

    /// <summary>
    /// The report's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The report's items.
    /// </summary>
    [JsonPropertyName("items")]
    public IList<int> Items { get; set; }
}