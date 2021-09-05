using System.Text.Json.Serialization;
using Universalis.Application.Common;

namespace Universalis.Application.Views
{
    /*
     * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
     * Please do not edit the field order unless it is unavoidable.
     */

    public class SaleView : IPriceable
    {
        /// <summary>
        /// Whether or not the item was high-quality.
        /// </summary>
        [JsonPropertyName("hq")]
        public bool Hq { get; init; }

        /// <summary>
        /// The price per unit sold.
        /// </summary>
        [JsonPropertyName("pricePerUnit")]
        public uint PricePerUnit { get; init; }

        /// <summary>
        /// The stack size sold.
        /// </summary>
        [JsonPropertyName("quantity")]
        public uint Quantity { get; init; }

        /// <summary>
        /// The sale time, in seconds since the UNIX epoch.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long TimestampUnixSeconds { get; init; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonPropertyName("worldName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string WorldName { get; init; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonPropertyName("worldID")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint? WorldId { get; init; }

        /// <summary>
        /// The buyer name.
        /// </summary>
        [JsonPropertyName("buyerName")]
        public string BuyerName { get; init; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonPropertyName("total")]
        public uint Total { get; init; }
    }
}