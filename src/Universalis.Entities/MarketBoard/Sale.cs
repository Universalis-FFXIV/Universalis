using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class Sale
    {
        [BsonElement("hq")]
        public bool Hq { get; init; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; init; }

        [BsonElement("quantity")]
        public uint Quantity { get; init; }

        [BsonElement("buyerName")]
        public string BuyerName { get; init; }

        [BsonElement("timestamp")]
        public uint TimestampUnixSeconds { get; init; }

        [BsonElement("uploadApplication")]
        public string UploadApplicationName { get; init; }
    }
}