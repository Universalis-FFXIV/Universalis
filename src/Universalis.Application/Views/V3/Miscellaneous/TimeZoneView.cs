using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Miscellaneous;

public class TimeZoneView
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("offset")]
    public double UtcOffset { get; set; }
    
    [JsonPropertyName("name")]
    public string FormattedName { get; set; }
}