using System;
using Universalis.Common.Caching;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class TaxRatesQuery : IEquatable<TaxRatesQuery>, ICopyable
{
    public uint WorldId { get; init; }

    public bool Equals(TaxRatesQuery other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return WorldId == other.WorldId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((TaxRatesQuery)obj);
    }

    public override int GetHashCode()
    {
        return (int)WorldId;
    }

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }

    public static bool operator ==(TaxRatesQuery left, TaxRatesQuery right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TaxRatesQuery left, TaxRatesQuery right)
    {
        return !Equals(left, right);
    }
}