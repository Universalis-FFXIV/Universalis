using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V3.Market;

public class MarketOverview
{
    /// <summary>
    /// The item's ID.
    /// </summary>
    [JsonPropertyName("item")]
    public uint ItemId { get; init; }

    /// <summary>
    /// The time that this item was last updated on each world requested, in milliseconds since the UNIX epoch.
    /// If an item has never been checked, the timestamp will be null.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public IDictionary<uint, long?> LastUpdateTimeUnixMilliseconds { get; init; }

    /// <summary>
    /// The current listings.
    /// </summary>
    [JsonPropertyName("listings")]
    public IList<Listing> Listings { get; init; }
    
    /// <summary>
    /// The known sales.
    /// </summary>
    [JsonPropertyName("sales")]
    public IList<Sale> Sales { get; init; }
}