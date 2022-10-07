using System;
using Universalis.Common.Caching;

namespace Universalis.Entities.MarketBoard;

public class MarketItem : ICopyable
{
    public uint WorldId { get; init; }

    public uint ItemId { get; init; }

    public DateTime LastUploadTime { get; set; }

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }
}