using System;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class CurrentlyShownQuery : IEquatable<CurrentlyShownQuery>
{
    public uint WorldId { get; init; }

    public uint ItemId { get; init; }
        
    public bool Equals(CurrentlyShownQuery other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return WorldId == other.WorldId && ItemId == other.ItemId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((CurrentlyShownQuery)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(WorldId, ItemId);
    }

    public static bool operator ==(CurrentlyShownQuery left, CurrentlyShownQuery right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CurrentlyShownQuery left, CurrentlyShownQuery right)
    {
        return !Equals(left, right);
    }
}