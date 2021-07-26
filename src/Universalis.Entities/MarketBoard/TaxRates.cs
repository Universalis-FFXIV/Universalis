using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class TaxRates : ExtraData
    {
        [BsonElement("limsaLominsa")]
        public byte LimsaLominsa { get; set; }

        [BsonElement("gridania")]
        public byte Gridania { get; set; }

        [BsonElement("uldah")]
        public byte Uldah { get; set; }

        [BsonElement("ishgard")]
        public byte Ishgard { get; set; }

        [BsonElement("kugane")]
        public byte Kugane { get; set; }

        [BsonElement("crystarium")]
        public byte Crystarium { get; set; }

        [BsonElement("uploaderID")]
        public string UploadIdHash { get; set; }

        [BsonElement("worldID")]
        public uint WorldId { get; set; }

        [BsonElement("sourceName")]
        public string UploadApplicationName { get; set; }
    }
}