using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.Extra.Stats;

public class LeastRecentlyUpdatedItemsView
{
    /// <summary>
    /// A list of item upload information in timestamp-ascending order.
    /// </summary>
    [JsonPropertyName("items")]
    public List<WorldItemRecencyView> Items { get; init; } = new();
}