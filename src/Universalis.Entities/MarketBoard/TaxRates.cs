using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Universalis.Entities.MarketBoard
{
    public class TaxRates : ExtraData, IEquatable<TaxRates>
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

        public bool Equals(TaxRates other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return LimsaLominsa == other.LimsaLominsa
                   && Gridania == other.Gridania
                   && Uldah == other.Uldah
                   && Ishgard == other.Ishgard
                   && Kugane == other.Kugane
                   && Crystarium == other.Crystarium
                   && WorldId == other.WorldId;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TaxRates)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(LimsaLominsa);
            hashCode.Add(Gridania);
            hashCode.Add(Uldah);
            hashCode.Add(Ishgard);
            hashCode.Add(Kugane);
            hashCode.Add(Crystarium);
            hashCode.Add(WorldId);
            return hashCode.ToHashCode();
        }
    }
}