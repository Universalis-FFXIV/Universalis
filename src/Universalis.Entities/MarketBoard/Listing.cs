using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard
{
    public class Listing
    {
        [BsonElement("listingID")]
        public string ListingId { get; set; }

        [BsonElement("hq")]
        public bool Hq { get; set; }

        [BsonElement("onMannequin")]
        public bool OnMannequin { get; set; }

        [BsonElement("materia")]
        public List<Materia> Materia { get; set; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [BsonElement("quantity")]
        public uint Quantity { get; set; }

        [BsonElement("stainID")]
        public byte DyeId { get; set; }

        [BsonElement("creatorID")]
        public string CreatorIdHash { get; set; }

        [BsonElement("creatorName")]
        public string CreatorName { get; set; }

        [BsonElement("lastReviewTime")]
        public uint LastReviewTime { get; set; }

        [BsonElement("retainerID")]
        public string RetainerId { get; set; }

        [BsonElement("retainerName")]
        public string RetainerName { get; set; }

        [BsonElement("retainerCity")]
        public byte RetainerCityId { get; set; }

        [BsonElement("sellerID")]
        public string SellerIdHash { get; set; }

        [BsonElement("sourceName")]
        public byte UploadApplicationName { get; set; }
    }
}