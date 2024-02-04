using System;

namespace Universalis.Entities.MarketBoard;

public class Sale : IEquatable<Sale>
{
    public Guid Id { get; init; }

    public int WorldId { get; init; }

    public int ItemId { get; init; }

    public bool Hq { get; init; }

    public int PricePerUnit { get; init; }

    // Quantities before December 2019 or so weren't stored here, and therefore will be null
    public int? Quantity { get; init; }

    // Names before May 22, 2022 weren't stored here, and therefore will be null
    public string BuyerName { get; init; }

    // Values before June 26, 2022 weren't stored here, and therefore will be null
    public bool? OnMannequin { get; init; }

    public DateTime SaleTime { get; set; }

    public string UploaderIdHash { get; init; }

    public bool Equals(Sale other)
    {
        if (other is null) return false;
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
        if (obj is null) return false;
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