using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.Extra.Stats;

public class UploadCountHistoryView
{
    /// <summary>
    /// The list of upload counts per day, over the past 30 days.
    /// </summary>
    [JsonPropertyName("uploadCountByDay")]
    public IList<double> UploadCountByDay { get; init; }
}