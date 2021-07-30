using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class TaxRates : ExtraData
    {
        public static readonly string DefaultSetName = "taxRates";

        [BsonElement("limsaLominsa")]
        public byte LimsaLominsa { get; init; }

        [BsonElement("gridania")]
        public byte Gridania { get; init; }

        [BsonElement("uldah")]
        public byte Uldah { get; init; }

        [BsonElement("ishgard")]
        public byte Ishgard { get; init; }

        [BsonElement("kugane")]
        public byte Kugane { get; init; }

        [BsonElement("crystarium")]
        public byte Crystarium { get; init; }

        [BsonElement("uploaderID")]
        public string UploaderIdHash { get; init; }

        [BsonElement("worldID")]
        public uint WorldId { get; init; }

        [BsonElement("sourceName")]
        public string UploadApplicationName { get; init; }

        public TaxRates() : base(DefaultSetName) { }
    }
}