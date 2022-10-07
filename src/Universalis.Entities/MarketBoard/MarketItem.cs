using System;

namespace Universalis.Entities.MarketBoard;

public class MarketItem
{
    public uint WorldId { get; init; }
    
    public uint ItemId { get; init; }

    public DateTime LastUploadTime { get; set; }
}