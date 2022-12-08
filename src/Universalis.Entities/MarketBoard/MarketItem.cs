using System;
using Universalis.Common.Caching;

namespace Universalis.Entities.MarketBoard;

public class MarketItem : ICopyable
{
    public int ItemId { get; init; }

    public int WorldId { get; init; }

    public DateTime LastUploadTime { get; set; }

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }
}