using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1.Extra;

public class ContentView
{
    /// <summary>
    /// The content ID of the object.
    /// </summary>
    [JsonPropertyName("contentID")]
    public string ContentId { get; init; }

    /// <summary>
    /// The content type of this object.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; init; }

    /// <summary>
    /// The character name associated with this character object, if this is one.
    /// </summary>
    [JsonPropertyName("characterName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string CharacterName { get; init; }
}