using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    /*
     * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
     * Please do not edit the field order unless it is unavoidable.
     */

    public class SaleView
    {
        /// <summary>
        /// Whether or not the item was high-quality.
        /// </summary>
        [JsonProperty("hq")]
        public bool Hq { get; set; }

        /// <summary>
        /// The price per unit sold.
        /// </summary>
        [JsonProperty("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        /// <summary>
        /// The stack size sold.
        /// </summary>
        [JsonProperty("quantity")]
        public uint Quantity { get; set; }

        /// <summary>
        /// The sale time, in seconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("timestamp")]
        public long TimestampUnixSeconds { get; set; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        /// <summary>
        /// The buyer name.
        /// </summary>
        [JsonProperty("buyerName")]
        public string BuyerName { get; set; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonProperty("total")]
        public uint Total { get; set; }
    }
}