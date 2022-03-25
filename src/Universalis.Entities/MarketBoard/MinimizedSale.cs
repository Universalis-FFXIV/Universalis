using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard;

public class MinimizedSale : IEquatable<MinimizedSale>
{
    [BsonElement("hq")]
    public bool Hq { get; init; }

    [BsonElement("pricePerUnit")]
    public uint PricePerUnit { get; init; }

    // Quantities before December 2019 or so weren't stored here, and therefore will be null
    [BsonElement("quantity")]
    public uint? Quantity { get; init; }

    [BsonElement("timestamp")]
    public double SaleTimeUnixSeconds { get; init; }

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
        // The uploader ID hash is not included in the equality check
        // because it's metadata specific to Universalis, not the game.
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Hq == other.Hq
               && PricePerUnit == other.PricePerUnit
               && Quantity == other.Quantity
               && Math.Abs(SaleTimeUnixSeconds - other.SaleTimeUnixSeconds) < 0.1;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((MinimizedSale) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Hq, PricePerUnit, Quantity, SaleTimeUnixSeconds);
    }
}