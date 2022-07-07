using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1;
/*
 * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
 * Please do not edit the field order unless it is unavoidable.
 */

public class HistoryView
{
    /// <summary>
    /// The item ID.
    /// </summary>
    [JsonPropertyName("itemID")]
    public uint ItemId { get; init; }

    /// <summary>
    /// The world ID, if applicable.
    /// </summary>
    [JsonPropertyName("worldID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? WorldId { get; init; }

    /// <summary>
    /// The last upload time for this endpoint, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("lastUploadTime")]
    public long LastUploadTimeUnixMilliseconds { get; set; }

    /// <summary>
    /// The historical sales.
    /// </summary>
    [JsonPropertyName("entries")]
    public List<MinimizedSaleView> Sales { get; set; } = new();

    /// <summary>
    /// The DC name, if applicable.
    /// </summary>
    [JsonPropertyName("dcName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string DcName { get; init; }
    
    /// <summary>
    /// The region name, if applicable.
    /// </summary>
    [JsonPropertyName("regionName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string RegionName { get; init; }

    /// <summary>
    /// A map of quantities to sale counts, representing the number of sales of each quantity.
    /// </summary>
    [JsonPropertyName("stackSizeHistogram")]
    public SortedDictionary<int, int> StackSizeHistogram { get; init; } = new();

    /// <summary>
    /// A map of quantities to NQ sale counts, representing the number of sales of each quantity.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramNQ")]
    public SortedDictionary<int, int> StackSizeHistogramNq { get; init; } = new();

    /// <summary>
    /// A map of quantities to HQ sale counts, representing the number of sales of each quantity.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramHQ")]
    public SortedDictionary<int, int> StackSizeHistogramHq { get; init; } = new();

    /// <summary>
    /// The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
    /// </summary>
    [JsonPropertyName("regularSaleVelocity")]
    public float SaleVelocity { get; init; }

    /// <summary>
    /// The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
    /// </summary>
    [JsonPropertyName("nqSaleVelocity")]
    public float SaleVelocityNq { get; init; }

    /// <summary>
    /// The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
    /// </summary>
    [JsonPropertyName("hqSaleVelocity")]
    public float SaleVelocityHq { get; init; }

    /// <summary>
    /// The world name, if applicable.
    /// </summary>
    [JsonPropertyName("worldName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string WorldName { get; init; }
}