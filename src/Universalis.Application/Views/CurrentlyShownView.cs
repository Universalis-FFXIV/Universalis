using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class CurrentlyShownView
    {
        /// <summary>
        /// The currently-shown listings.
        /// </summary>
        [JsonProperty("listings", Order = 3)]
        public List<ListingView> Listings { get; set; } = new();

        /// <summary>
        /// The currently-shown sales.
        /// </summary>
        [JsonProperty("recentHistory", Order = 4)]
        public List<SaleView> RecentHistory { get; set; } = new();

        /// <summary>
        /// The item ID.
        /// </summary>
        [JsonProperty("itemID", Order = 0)]
        public uint ItemId { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore, Order = 1)]
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
        [JsonProperty("lastUploadTime", Order = 2)]
        public long LastUploadTimeUnixMilliseconds { get; set; }

        /// <summary>
        /// The average listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePrice", Order = 5)]
        public float CurrentAveragePrice { get; set; }

        /// <summary>
        /// The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePriceNQ", Order = 6)]
        public float CurrentAveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("currentAveragePriceHQ", Order = 7)]
        public float CurrentAveragePriceHq { get; set; }

        /// <summary>
        /// The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("regularSaleVelocity", Order = 8)]
        public float SaleVelocity { get; set; }

        /// <summary>
        /// The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("nqSaleVelocity", Order = 9)]
        public float SaleVelocityNq { get; set; }

        /// <summary>
        /// The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).
        /// </summary>
        [JsonProperty("hqSaleVelocity", Order = 10)]
        public float SaleVelocityHq { get; set; }

        /// <summary>
        /// The average sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePrice", Order = 11)]
        public float AveragePrice { get; set; }

        /// <summary>
        /// The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePriceNQ", Order = 12)]
        public float AveragePriceNq { get; set; }

        /// <summary>
        /// The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean.
        /// </summary>
        [JsonProperty("averagePriceHQ", Order = 13)]
        public float AveragePriceHq { get; set; }

        /// <summary>
        /// The minimum listing price.
        /// </summary>
        [JsonProperty("minPrice", Order = 14)]
        public uint MinPrice { get; set; }

        /// <summary>
        /// The minimum NQ listing price.
        /// </summary>
        [JsonProperty("minPriceNQ", Order = 15)]
        public uint MinPriceNq { get; set; }

        /// <summary>
        /// The minimum HQ listing price.
        /// </summary>
        [JsonProperty("minPriceHQ", Order = 16)]
        public uint MinPriceHq { get; set; }

        /// <summary>
        /// The maximum listing price.
        /// </summary>
        [JsonProperty("maxPrice", Order = 17)]
        public uint MaxPrice { get; set; }

        /// <summary>
        /// The maximum NQ listing price.
        /// </summary>
        [JsonProperty("maxPriceNQ", Order = 18)]
        public uint MaxPriceNq { get; set; }

        /// <summary>
        /// The maximum HQ listing price.
        /// </summary>
        [JsonProperty("maxPriceHQ", Order = 19)]
        public uint MaxPriceHq { get; set; }

        /// <summary>
        /// A map of quantities to listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogram", Order = 20)]
        public SortedDictionary<int, int> StackSizeHistogram { get; set; } = new();

        /// <summary>
        /// A map of quantities to NQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramNQ", Order = 21)]
        public SortedDictionary<int, int> StackSizeHistogramNq { get; set; } = new();

        /// <summary>
        /// A map of quantities to HQ listing counts, representing the number of listings of each quantity.
        /// </summary>
        [JsonProperty("stackSizeHistogramHQ", Order = 22)]
        public SortedDictionary<int, int> StackSizeHistogramHq { get; set; } = new();
    }
}