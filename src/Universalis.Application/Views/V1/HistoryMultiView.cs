using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1;
/*
 * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
 * Please do not edit the field order unless it is unavoidable.
 */

public class HistoryMultiView
{
    /// <summary>
    /// The item IDs that were requested.
    /// </summary>
    [JsonPropertyName("itemIDs")]
    public List<uint> ItemIds { get; init; } = new();

    /// <summary>
    /// The item data that was requested, as a list. Use the nested item IDs
    /// to pull the item you want, or consider using the v2 endpoint instead.
    /// </summary>
    [JsonPropertyName("items")]
    public List<HistoryView> Items { get; init; }

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