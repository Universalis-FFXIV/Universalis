using System;
using Universalis.Common.Caching;

namespace Universalis.Entities.MarketBoard;

public class TaxRates : IEquatable<TaxRates>, ICopyable
{
    public int LimsaLominsa { get; init; }

    public int Gridania { get; init; }

    public int Uldah { get; init; }

    public int Ishgard { get; init; }

    public int Kugane { get; init; }

    public int Crystarium { get; init; }

    public int OldSharlayan { get; init; }

    public string UploadApplicationName { get; init; }

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }

    public bool Equals(TaxRates other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return LimsaLominsa == other.LimsaLominsa && Gridania == other.Gridania && Uldah == other.Uldah &&
               Ishgard == other.Ishgard && Kugane == other.Kugane && Crystarium == other.Crystarium &&
               OldSharlayan == other.OldSharlayan && UploadApplicationName == other.UploadApplicationName;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((TaxRates)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LimsaLominsa, Gridania, Uldah, Ishgard, Kugane, Crystarium, OldSharlayan,
            UploadApplicationName);
    }

    public static bool operator ==(TaxRates left, TaxRates right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TaxRates left, TaxRates right)
    {
        return !Equals(left, right);
    }
}