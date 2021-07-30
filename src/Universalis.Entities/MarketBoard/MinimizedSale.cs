using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class MinimizedSale : IEquatable<MinimizedSale>
    {
        [BsonElement("hq")]
        public bool Hq { get; init; }

        [BsonElement("pricePerUnit")]
        public uint PricePerUnit { get; init; }

        [BsonElement("quantity")]
        public uint Quantity { get; init; }

        [BsonElement("timestamp")]
        public uint SaleTimeUnixSeconds { get; init; }

        [BsonElement("uploaderID")]
        public string UploaderIdHash { get; init; }

        public static MinimizedSale FromSale(Sale s, string uploaderIdHash)
        {
            return new()
            {
                Hq = s.Hq,
                PricePerUnit = s.PricePerUnit,
                Quantity = s.Quantity,
                SaleTimeUnixSeconds = s.TimestampUnixSeconds,
                UploaderIdHash = uploaderIdHash,
            };
        }

        public bool Equals(MinimizedSale other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hq == other.Hq
                   && PricePerUnit == other.PricePerUnit
                   && Quantity == other.Quantity
                   && SaleTimeUnixSeconds == other.SaleTimeUnixSeconds;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MinimizedSale) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hq, PricePerUnit, Quantity, SaleTimeUnixSeconds, UploaderIdHash);
        }
    }
}