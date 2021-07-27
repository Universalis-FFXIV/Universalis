using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universalis.Application.Views
{
    public class HistoryView
    {
        [JsonProperty("entries")]
        public List<MinimizedSaleView> Sales { get; set; }

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

        [JsonProperty("stackSizeHistogram")]
        public IDictionary<int, int> StackSizeHistogram { get; set; }

        [JsonProperty("stackSizeHistogramNQ")]
        public IDictionary<int, int> StackSizeHistogramNq { get; set; }

        [JsonProperty("stackSizeHistogramHQ")]
        public IDictionary<int, int> StackSizeHistogramHq { get; set; }

        [JsonProperty("regularSaleVelocity")]
        public float RegularSaleVelocity { get; set; }

        [JsonProperty("nqSaleVelocity")]
        public float RegularSaleVelocityNq { get; set; }

        [JsonProperty("hqSaleVelocity")]
        public float RegularSaleVelocityHq { get; set; }
    }
}