using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserCharacterView
{
    /// <summary>
    /// The ID of this character.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The Lodestone ID of this character.
    /// </summary>
    [JsonPropertyName("lodestoneID")]
    public string LodestoneId { get; set; }

    /// <summary>
    /// The time that this character was updated, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("updated")]
    public string UpdatedTimestampMs { get; set; }

    /// <summary>
    /// The name of this character.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The server of this character.
    /// </summary>
    [JsonPropertyName("server")]
    public string Server { get; set; }

    /// <summary>
    /// The avatar for this character.
    /// </summary>
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    /// <summary>
    /// Whether or not this character is the user's main character.
    /// </summary>
    [JsonPropertyName("main")]
    public bool Main { get; set; }

    /// <summary>
    /// Whether or not ownership of this character has been confirmed.
    /// </summary>
    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }
}