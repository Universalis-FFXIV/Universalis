using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universalis.Application.Views
{
    public class ListingView
    {
        /// <summary>
        /// The time that this listing was posted, in seconds since the UNIX epoch.
        /// </summary>
        [JsonProperty("lastReviewTime")]
        public long LastReviewTimeUnixSeconds { get; set; }

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
        /// The ID of the dye on this item.
        /// </summary>
        [JsonProperty("stainID")]
        public uint DyeId { get; set; }

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
        /// The creator's character name.
        /// </summary>
        [JsonProperty("creatorName")]
        public string CreatorName { get; set; }

        /// <summary>
        /// A SHA256 hash of the creator's ID.
        /// </summary>
        [JsonProperty("creatorID")]
        public string CreatorIdHash { get; set; }

        /// <summary>
        /// Whether or not the item is high-quality.
        /// </summary>
        [JsonProperty("hq")]
        public bool Hq { get; set; }

        /// <summary>
        /// Whether or not the item is crafted.
        /// </summary>
        [JsonProperty("isCrafted")]
        public bool IsCrafted { get; set; }

        /// <summary>
        /// The ID of this listing. Due to some current client-side bugs, this will almost always be null.
        /// </summary>
        [JsonProperty("listingID")]
        public string ListingId { get; set; }

        /// <summary>
        /// The materia on this item.
        /// </summary>
        [JsonProperty("materia")]
        public List<MateriaView> Materia { get; set; } = new();

        /// <summary>
        /// Whether or not the item is being sold on a mannequin.
        /// </summary>
        [JsonProperty("onMannequin")]
        public bool OnMannequin { get; set; }

        /// <summary>
        /// The city ID of the retainer.
        /// Limsa Lominsa = 1
        /// Gridania = 2
        /// Ul'dah = 3
        /// Ishgard = 4
        /// Kugane = 7
        /// Crystarium = 10
        /// </summary>
        [JsonProperty("retainerCity")]
        public int RetainerCityId { get; set; }

        /// <summary>
        /// The retainer's ID.
        /// </summary>
        [JsonProperty("retainerID")]
        public string RetainerId { get; set; }

        /// <summary>
        /// The retainer's name.
        /// </summary>
        [JsonProperty("retainerName")]
        public string RetainerName { get; set; }

        /// <summary>
        /// A SHA256 hash of the seller's ID.
        /// </summary>
        [JsonProperty("sellerID")]
        public string SellerIdHash { get; set; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonProperty("total")]
        public uint Total { get; set; }
    }
}