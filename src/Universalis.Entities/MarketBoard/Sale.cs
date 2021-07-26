using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class Sale
    {
        [BsonElement("hq")]
        public bool Hq { get; set; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [BsonElement("quantity")]
        public uint Quantity { get; set; }

        [BsonElement("buyerName")]
        public string BuyerName { get; set; }

        [BsonElement("timestamp")]
        public uint TimestampUnixSeconds { get; set; }

        [BsonElement("uploadApplication")]
        public bool UploadApplicationName { get; set; }
    }
}