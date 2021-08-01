﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class CurrentlyShownView
    {
        [JsonProperty("listings")]
        public List<ListingView> Listings { get; set; } = new();

        [JsonProperty("recentHistory")]
        public List<SaleView> RecentHistory { get; set; } = new();

        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }

        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        [JsonProperty("lastUploadTime")]
        public uint LastUploadTimeUnixMilliseconds { get; set; }

        [JsonProperty("currentAveragePrice")]
        public float CurrentAveragePrice { get; set; }

        [JsonProperty("currentAveragePriceNQ")]
        public float CurrentAveragePriceNq { get; set; }

        [JsonProperty("currentAveragePriceHQ")]
        public float CurrentAveragePriceHq { get; set; }

        [JsonProperty("regularSaleVelocity")]
        public float SaleVelocity { get; set; }

        [JsonProperty("nqSaleVelocity")]
        public float SaleVelocityNq { get; set; }

        [JsonProperty("hqSaleVelocity")]
        public float SaleVelocityHq { get; set; }

        [JsonProperty("averagePrice")]
        public float AveragePrice { get; set; }

        [JsonProperty("averagePriceNQ")]
        public float AveragePriceNq { get; set; }

        [JsonProperty("averagePriceHQ")]
        public float AveragePriceHq { get; set; }

        [JsonProperty("minPrice")]
        public uint MinPrice { get; set; }

        [JsonProperty("minPriceNQ")]
        public uint MinPriceNq { get; set; }

        [JsonProperty("minPriceHQ")]
        public uint MinPriceHq { get; set; }

        [JsonProperty("maxPrice")]
        public uint MaxPrice { get; set; }

        [JsonProperty("maxPriceNQ")]
        public uint MaxPriceNq { get; set; }

        [JsonProperty("maxPriceHQ")]
        public uint MaxPriceHq { get; set; }

        [JsonProperty("stackSizeHistogram")]
        public SortedDictionary<int, int> StackSizeHistogram { get; set; } = new();

        [JsonProperty("stackSizeHistogramNQ")]
        public SortedDictionary<int, int> StackSizeHistogramNq { get; set; } = new();

        [JsonProperty("stackSizeHistogramHQ")]
        public SortedDictionary<int, int> StackSizeHistogramHq { get; set; } = new();
    }
}