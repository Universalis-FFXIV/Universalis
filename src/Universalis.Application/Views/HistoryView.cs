using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universalis.Application.Views
{
    public class HistoryView
    {
        /// <summary>
        /// The historical sales.
        /// </summary>
        [JsonProperty("entries")]
        public List<MinimizedSaleView> Sales { get; set; } = new();

        /// <summary>
        /// The item ID.
        /// </summary>
        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }

        /// <summary>
        /// The DC name, if applicable.
        /// </summary>
        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        /// <summary>
        /// The last upload time for this endpoint, in milliseconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("lastUploadTime")]
        public double LastUploadTimeUnixMilliseconds { get; set; }

        /// <summary>
        /// A map of quantities to sale counts, representing the number of sales of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogram")]
        public SortedDictionary<int, int> StackSizeHistogram { get; set; } = new();

        /// <summary>
        /// A map of quantities to NQ sale counts, representing the number of sales of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramNQ")]
        public SortedDictionary<int, int> StackSizeHistogramNq { get; set; } = new();

        /// <summary>
        /// A map of quantities to HQ sale counts, representing the number of sales of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramHQ")]
        public SortedDictionary<int, int> StackSizeHistogramHq { get; set; } = new();

        /// <summary>
        /// The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("regularSaleVelocity")]
        public float SaleVelocity { get; set; }

        /// <summary>
        /// The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("nqSaleVelocity")]
        public float SaleVelocityNq { get; set; }

        /// <summary>
        /// The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("hqSaleVelocity")]
        public float SaleVelocityHq { get; set; }
    }
}