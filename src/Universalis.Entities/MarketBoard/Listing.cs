using System;
using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class Listing : IEquatable<Listing>
{
    public string ListingId { get; init; }

    public bool Hq { get; init; }

    public bool OnMannequin { get; init; }

    public List<Materia> Materia { get; init; }

    public int PricePerUnit { get; set; }

    public int Quantity { get; init; }

    public int DyeId { get; init; }

    public string CreatorId { get; init; }

    public string CreatorName { get; init; }

    public DateTime LastReviewTime { get; init; }

    public string RetainerId { get; init; }

    public string RetainerName { get; init; }

    public int RetainerCityId { get; init; }

    public string SellerId { get; init; }

    public int ItemId { get; set; }

    public int WorldId { get; set; }

    public DateTime UpdatedAt { get; init; }

    public string Source { get; set; }

    public bool Equals(Listing other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ListingId == other.ListingId &&
               Hq == other.Hq &&
               OnMannequin == other.OnMannequin &&
               PricePerUnit == other.PricePerUnit &&
               Quantity == other.Quantity &&
               DyeId == other.DyeId &&
               CreatorId == other.CreatorId &&
               CreatorName == other.CreatorName &&
               LastReviewTime.Equals(other.LastReviewTime) &&
               RetainerId == other.RetainerId &&
               RetainerName == other.RetainerName &&
               RetainerCityId == other.RetainerCityId &&
               SellerId == other.SellerId &&
               ItemId == other.ItemId &&
               WorldId == other.WorldId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Listing)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(ListingId);
        hashCode.Add(Hq);
        hashCode.Add(OnMannequin);
        hashCode.Add(PricePerUnit);
        hashCode.Add(Quantity);
        hashCode.Add(DyeId);
        hashCode.Add(CreatorId);
        hashCode.Add(CreatorName);
        hashCode.Add(LastReviewTime);
        hashCode.Add(RetainerId);
        hashCode.Add(RetainerName);
        hashCode.Add(RetainerCityId);
        hashCode.Add(SellerId);
        hashCode.Add(ItemId);
        hashCode.Add(WorldId);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(Listing left, Listing right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Listing left, Listing right)
    {
        return !Equals(left, right);
    }
}