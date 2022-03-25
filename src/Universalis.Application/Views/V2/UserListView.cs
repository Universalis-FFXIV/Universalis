using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserListView
{
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
    /// The IDs of the list items.
    /// </summary>
    [JsonPropertyName("itemIDs")] 
    public IList<int> Items { get; set; }
}