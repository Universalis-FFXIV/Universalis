using System;

namespace Universalis.Application.Caching;

public class CachedCurrentlyShownQuery : IEquatable<CachedCurrentlyShownQuery>, ICopyable
{
    public uint WorldId { get; init; }

    public uint ItemId { get; init; }

    public bool Equals(CachedCurrentlyShownQuery other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return WorldId == other.WorldId && ItemId == other.ItemId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((CachedCurrentlyShownQuery)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(WorldId, ItemId);
    }

    public ICopyable Clone()
    {
        return (CachedCurrentlyShownQuery)MemberwiseClone();
    }

    public static bool operator ==(CachedCurrentlyShownQuery left, CachedCurrentlyShownQuery right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CachedCurrentlyShownQuery left, CachedCurrentlyShownQuery right)
    {
        return !Equals(left, right);
    }
}