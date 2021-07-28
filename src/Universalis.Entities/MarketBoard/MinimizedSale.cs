using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class MinimizedSale
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

        protected bool Equals(MinimizedSale other)
        {
            return Hq == other.Hq && PricePerUnit == other.PricePerUnit && Quantity == other.Quantity && SaleTimeUnixSeconds == other.SaleTimeUnixSeconds && UploaderIdHash == other.UploaderIdHash;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((MinimizedSale)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hq, PricePerUnit, Quantity, SaleTimeUnixSeconds, UploaderIdHash);
        }

        public static MinimizedSale FromSale(Sale s)
        {
            return new()
            {
                Hq = s.Hq,
                PricePerUnit = s.PricePerUnit,
                Quantity = s.Quantity,
                SaleTimeUnixSeconds = s.TimestampUnixSeconds,
                UploaderIdHash = s.UploaderIdHash,
            };
        }

        public static bool operator ==(MinimizedSale lhs, MinimizedSale rhs)
        {
            return lhs?.Hq == rhs?.Hq
                   && lhs?.PricePerUnit == rhs?.PricePerUnit
                   && lhs?.Quantity == rhs?.Quantity
                   && lhs?.SaleTimeUnixSeconds == rhs?.SaleTimeUnixSeconds;
        }

        public static bool operator !=(MinimizedSale lhs, MinimizedSale rhs)
        {
            return !(lhs == rhs);
        }
    }
}