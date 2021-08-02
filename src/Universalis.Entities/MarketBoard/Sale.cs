using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard
{
    public class Sale : IEquatable<Sale>
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

        public bool Equals(Sale other)
        {
            // The upload application is not included in the equality check
            // because it's metadata specific to Universalis, not the game.
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hq == other.Hq
                   && PricePerUnit == other.PricePerUnit
                   && Quantity == other.Quantity
                   && BuyerName == other.BuyerName
                   && TimestampUnixSeconds == other.TimestampUnixSeconds;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Sale) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hq, PricePerUnit, Quantity, BuyerName, TimestampUnixSeconds);
        }
    }
}