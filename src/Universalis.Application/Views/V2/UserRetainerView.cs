using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserRetainerView
{
    /// <summary>
    /// The retainer's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The time that this retainer was added, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("added")]
    public string AddedTimestampMs { get; set; }

    /// <summary>
    /// The time that this retainer was updated, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("updated")]
    public string UpdatedTimestampMs { get; set; }

    /// <summary>
    /// The name of this retainer.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The server of this retainer.
    /// </summary>
    [JsonPropertyName("server")]
    public string Server { get; set; }

    /// <summary>
    /// The avatar for this retainer.
    /// </summary>
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    /// <summary>
    /// Whether or not ownership of this retainer has been confirmed.
    /// </summary>
    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }
}