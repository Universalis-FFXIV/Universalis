using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard
{
    public class Listing
    {
        [BsonElement("listingID")]
        public string ListingId { get; init; }

        [BsonElement("hq")]
        public bool Hq { get; init; }

        [BsonElement("onMannequin")]
        public bool OnMannequin { get; init; }

        [BsonElement("materia")]
        public List<Materia> Materia { get; init; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; init; }

        [BsonElement("quantity")]
        public uint Quantity { get; init; }

        [BsonElement("stainID")]
        public uint DyeId { get; init; }

        [BsonElement("creatorID")]
        public string CreatorIdHash { get; init; }

        [BsonElement("creatorName")]
        public string CreatorName { get; init; }

        [BsonElement("lastReviewTime")]
        public uint LastReviewTimeUnixSeconds { get; init; }

        [BsonElement("retainerID")]
        public string RetainerId { get; init; }

        [BsonElement("retainerName")]
        public string RetainerName { get; init; }

        [BsonElement("retainerCity")]
        public byte RetainerCityId { get; init; }

        [BsonElement("sellerID")]
        public string SellerIdHash { get; init; }

        [BsonElement("sourceName")]
        public string UploadApplicationName { get; init; }
    }
}