using System.Text.Json.Serialization;

namespace Universalis.Application.Views.Extra.Stats;

public class WorldItemRecencyView
{
    /// <summary>
    /// The item ID.
    /// </summary>
    [JsonPropertyName("itemID")]
    public uint ItemId { get; init; }

    /// <summary>
    /// The last upload time for the item on the listed world.
    /// </summary>
    [JsonPropertyName("lastUploadTime")]
    public double LastUploadTimeUnixMilliseconds { get; init; }

    /// <summary>
    /// The world ID.
    /// </summary>
    [JsonPropertyName("worldID")]
    public uint WorldId { get; init; }

    /// <summary>
    /// The world name.
    /// </summary>
    [JsonPropertyName("worldName")]
    public string WorldName { get; init; }
}