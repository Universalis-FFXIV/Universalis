using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.MarketBoard;

public class Sale : IEquatable<Sale>
{
    [BsonElement("hq")]
    public bool Hq { get; init; }

    [BsonElement("pricePerUnit")]
    public uint PricePerUnit { get; init; }

    // Quantities before December 2019 or so weren't stored here, and therefore will be null
    [BsonElement("quantity")]
    public uint? Quantity { get; init; }
    
    [BsonElement("buyerName")]
    public string BuyerName { get; init; }

    [BsonElement("timestamp")]
    public double TimestampUnixSeconds { get; init; }

    [BsonElement("uploaderID")]
    public string UploaderIdHash { get; init; }

    public bool Equals(Sale other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Hq == other.Hq
               && PricePerUnit == other.PricePerUnit
               && Quantity == other.Quantity
               && BuyerName == other.BuyerName
               && TimestampUnixSeconds.Equals(other.TimestampUnixSeconds);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Sale)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Hq, PricePerUnit, Quantity, BuyerName, TimestampUnixSeconds, UploaderIdHash);
    }

    public static bool operator ==(Sale left, Sale right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Sale left, Sale right)
    {
        return !Equals(left, right);
    }
}