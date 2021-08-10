using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class CurrentlyShownView
    {
        /*
         * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
         * Please do not edit the field order unless it is unavoidable.
         */

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
        /// The last upload time for this endpoint, in milliseconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("lastUploadTime")]
        public long LastUploadTimeUnixMilliseconds { get; set; }

        /// <summary>
        /// The currently-shown listings.
        /// </summary>
        [JsonProperty("listings")]
        public List<ListingView> Listings { get; set; } = new();

        /// <summary>
        /// The currently-shown sales.
        /// </summary>
        [JsonProperty("recentHistory")]
        public List<SaleView> RecentHistory { get; set; } = new();

        /// <summary>
        /// The DC name, if applicable.
        /// </summary>
        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        /// <summary>
        /// The average listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePrice")]
        public float CurrentAveragePrice { get; set; }

        /// <summary>
        /// The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePriceNQ")]
        public float CurrentAveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePriceHQ")]
        public float CurrentAveragePriceHq { get; set; }

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

        /// <summary>
        /// The average sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePrice")]
        public float AveragePrice { get; set; }

        /// <summary>
        /// The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePriceNQ")]
        public float AveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePriceHQ")]
        public float AveragePriceHq { get; set; }

        /// <summary>
        /// The minimum listing price.
        /// </summary>
        [JsonProperty("minPrice")]
        public uint MinPrice { get; set; }

        /// <summary>
        /// The minimum NQ listing price.
        /// </summary>
        [JsonProperty("minPriceNQ")]
        public uint MinPriceNq { get; set; }

        /// <summary>
        /// The minimum HQ listing price.
        /// </summary>
        [JsonProperty("minPriceHQ")]
        public uint MinPriceHq { get; set; }

        /// <summary>
        /// The maximum listing price.
        /// </summary>
        [JsonProperty("maxPrice")]
        public uint MaxPrice { get; set; }

        /// <summary>
        /// The maximum NQ listing price.
        /// </summary>
        [JsonProperty("maxPriceNQ")]
        public uint MaxPriceNq { get; set; }

        /// <summary>
        /// The maximum HQ listing price.
        /// </summary>
        [JsonProperty("maxPriceHQ")]
        public uint MaxPriceHq { get; set; }

        /// <summary>
        /// A map of quantities to listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogram")]
        public SortedDictionary<int, int> StackSizeHistogram { get; set; } = new();

        /// <summary>
        /// A map of quantities to NQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramNQ")]
        public SortedDictionary<int, int> StackSizeHistogramNq { get; set; } = new();

        /// <summary>
        /// A map of quantities to HQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramHQ")]
        public SortedDictionary<int, int> StackSizeHistogramHq { get; set; } = new();

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }
    }
}