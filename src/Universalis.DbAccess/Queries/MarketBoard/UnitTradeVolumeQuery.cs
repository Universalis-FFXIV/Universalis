using System;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class UnitTradeVolumeQuery
{
    public uint WorldId { get; init; }

    public uint ItemId { get; set; }

    public DateTime From { get; set; }
}
