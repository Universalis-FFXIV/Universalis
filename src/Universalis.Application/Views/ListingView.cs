using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Views
{
    /*
     * Note for anyone viewing this file: People rely on the field order (even though JSON is defined to be unordered).
     * Please do not edit the field order unless it is unavoidable.
     */

    public class ListingView : IPriceable
    {
        /// <summary>
        /// The time that this listing was posted, in seconds since the UNIX epoch.
        /// </summary>
        [JsonPropertyName("lastReviewTime")]
        public long LastReviewTimeUnixSeconds { get; init; }

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
        /// The ID of the dye on this item.
        /// </summary>
        [JsonPropertyName("stainID")]
        public uint DyeId { get; init; }

        /// <summary>
        /// The world name, if applicable.
        /// </summary>
        [JsonPropertyName("worldName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string WorldName { get; set; }

        /// <summary>
        /// The world ID, if applicable.
        /// </summary>
        [JsonPropertyName("worldID")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint? WorldId { get; set; }

        /// <summary>
        /// The creator's character name.
        /// </summary>
        [JsonPropertyName("creatorName")]
        public string CreatorName { get; init; }

        /// <summary>
        /// A SHA256 hash of the creator's ID.
        /// </summary>
        [JsonPropertyName("creatorID")]
        public string CreatorIdHash { get; set; }

        /// <summary>
        /// Whether or not the item is high-quality.
        /// </summary>
        [JsonPropertyName("hq")]
        public bool Hq { get; init; }

        /// <summary>
        /// Whether or not the item is crafted.
        /// </summary>
        [JsonPropertyName("isCrafted")]
        public bool IsCrafted { get; init; }

        /// <summary>
        /// The ID of this listing. Due to some current client-side bugs, this will almost always be null.
        /// </summary>
        [JsonPropertyName("listingID")]
        public string ListingId { get; set; }

        /// <summary>
        /// The materia on this item.
        /// </summary>
        [JsonPropertyName("materia")]
        public List<MateriaView> Materia { get; init; } = new();

        /// <summary>
        /// Whether or not the item is being sold on a mannequin.
        /// </summary>
        [JsonPropertyName("onMannequin")]
        public bool OnMannequin { get; init; }

        /// <summary>
        /// The city ID of the retainer.
        /// Limsa Lominsa = 1
        /// Gridania = 2
        /// Ul'dah = 3
        /// Ishgard = 4
        /// Kugane = 7
        /// Crystarium = 10
        /// </summary>
        [JsonPropertyName("retainerCity")]
        public int RetainerCityId { get; init; }

        /// <summary>
        /// The retainer's ID.
        /// </summary>
        [JsonPropertyName("retainerID")]
        public string RetainerId { get; set; }

        /// <summary>
        /// The retainer's name.
        /// </summary>
        [JsonPropertyName("retainerName")]
        public string RetainerName { get; init; }

        /// <summary>
        /// A SHA256 hash of the seller's ID.
        /// </summary>
        [JsonPropertyName("sellerID")]
        public string SellerIdHash { get; set; }

        /// <summary>
        /// The total price.
        /// </summary>
        [JsonPropertyName("total")]
        public uint Total { get; init; }

        public static async Task<ListingView> FromListing(Listing l, CancellationToken cancellationToken = default)
        {
            var ppuWithGst = (uint)Math.Ceiling(l.PricePerUnit * 1.05);
            var listingView = new ListingView
            {
                Hq = l.Hq,
                OnMannequin = l.OnMannequin,
                Materia = l.Materia?
                    .Select(m => new MateriaView
                    {
                        SlotId = m.SlotId,
                        MateriaId = m.MateriaId,
                    })
                    .ToList() ?? new List<MateriaView>(),
                PricePerUnit = ppuWithGst,
                Quantity = l.Quantity,
                Total = ppuWithGst * l.Quantity,
                DyeId = l.DyeId,
                CreatorName = l.CreatorName ?? "",
                IsCrafted = !string.IsNullOrEmpty(l.CreatorName),
                LastReviewTimeUnixSeconds = (long)l.LastReviewTimeUnixSeconds,
                RetainerName = l.RetainerName,
                RetainerCityId = l.RetainerCityId,
            };

            using var sha256 = SHA256.Create();

            if (!string.IsNullOrEmpty(l.CreatorId))
            {
                listingView.CreatorIdHash = await Util.Hash(sha256, l.CreatorId, cancellationToken);
            }

            if (!string.IsNullOrEmpty(l.ListingId))
            {
                listingView.ListingId = await Util.Hash(sha256, l.ListingId, cancellationToken);
            }

            listingView.SellerIdHash = await Util.Hash(sha256, l.SellerId, cancellationToken);
            listingView.RetainerId = await Util.Hash(sha256, l.RetainerId, cancellationToken);

            return listingView;
        }
    }
}