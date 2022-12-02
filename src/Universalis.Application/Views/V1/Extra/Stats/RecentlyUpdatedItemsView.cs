using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1.Extra.Stats;

public class RecentlyUpdatedItemsView
{
    /// <summary>
    /// A list of item IDs, with the most recent first.
    /// </summary>
    [JsonPropertyName("items")]
    public List<int> Items { get; init; } = new();
}