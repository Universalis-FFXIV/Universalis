using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class MinimizedSale
    {
        [BsonElement("hq")]
        public bool Hq { get; set; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; set; }

        [BsonElement("quantity")]
        public uint Quantity { get; set; }

        [BsonElement("timestamp")]
        public uint SaleTimeUnixSeconds { get; set; }

        [BsonElement("uploaderID")]
        public string UploaderIdHash { get; set; }
    }
}