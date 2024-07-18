using System;
using MemoryPack;
using Universalis.Common.Caching;

namespace Universalis.Entities.MarketBoard;

[MemoryPackable]
public partial class MarketItem : ICopyable
{
    public int ItemId { get; init; }

    public int WorldId { get; init; }

    public DateTime LastUploadTime { get; set; }

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }
}