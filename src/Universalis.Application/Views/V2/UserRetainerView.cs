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
    /// The time that this list was created, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("created")]
    public string CreatedTimestampMs { get; set; }

    /// <summary>
    /// The time that this list was updated, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("updated")]
    public string UpdatedTimestampMs { get; set; }

    /// <summary>
    /// The name of this list.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The name of this list.
    /// </summary>
    [JsonPropertyName("server")]
    public string Server { get; set; }

    /// <summary>
    /// The name of this list.
    /// </summary>
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    /// <summary>
    /// The name of this list.
    /// </summary>
    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }
}