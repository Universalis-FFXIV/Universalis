using System;

namespace Universalis.Entities.MarketBoard;

public class Sale : IEquatable<Sale>
{
    public Guid Id { get; init; }
    
    public uint WorldId { get; init; }
    
    public uint ItemId { get; init; }
    
    public bool Hq { get; init; }

    public uint PricePerUnit { get; init; }

    // Quantities before December 2019 or so weren't stored here, and therefore will be null
    public uint? Quantity { get; init; }
    
    public string BuyerName { get; init; }

    public DateTime SaleTime { get; init; }

    public string UploaderIdHash { get; init; }

    public bool Equals(Sale other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return WorldId == other.WorldId
               && ItemId == other.ItemId
               && Hq == other.Hq
               && PricePerUnit == other.PricePerUnit
               && Quantity == other.Quantity
               && BuyerName == other.BuyerName
               && SaleTime.Equals(other.SaleTime);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Sale)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(WorldId, ItemId, Hq, PricePerUnit, Quantity, BuyerName, SaleTime, UploaderIdHash);
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