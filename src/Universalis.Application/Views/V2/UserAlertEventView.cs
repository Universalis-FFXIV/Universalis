using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserAlertEventView
{
    /// <summary>
    /// The alert event's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The ID of the alert corresponding to this event.
    /// </summary>
    [JsonPropertyName("alertID")]
    public string AlertId { get; set; }

    /// <summary>
    /// The alert event's timestamp, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string TimestampMs { get; set; }

    /// <summary>
    /// The alert event's payload.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; }
}