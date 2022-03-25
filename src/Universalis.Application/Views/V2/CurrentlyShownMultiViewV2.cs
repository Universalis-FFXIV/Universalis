using System.Collections.Generic;
using System.Text.Json.Serialization;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Views.V2;

public class CurrentlyShownMultiViewV2
{
    /// <summary>
    /// The item IDs that were requested.
    /// </summary>
    [JsonPropertyName("itemIDs")]
    public List<uint> ItemIds { get; init; } = new();

    /// <summary>
    /// The item data that was requested, keyed on the item ID.
    /// </summary>
    [JsonPropertyName("items")]
    public Dictionary<uint, CurrentlyShownView> Items { get; init; } = new();

    /// <summary>
    /// The ID of the world requested, if applicable.
    /// </summary>
    [JsonPropertyName("worldID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? WorldId { get; init; }

    /// <summary>
    /// The name of the DC requested, if applicable.
    /// </summary>
    [JsonPropertyName("dcName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string DcName { get; init; }

    /// <summary>
    /// A list of IDs that could not be resolved to any item data.
    /// </summary>
    [JsonPropertyName("unresolvedItems")]
    public uint[] UnresolvedItemIds { get; init; }

    /// <summary>
    /// The name of the world requested, if applicable.
    /// </summary>
    [JsonPropertyName("worldName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string WorldName { get; init; }
}