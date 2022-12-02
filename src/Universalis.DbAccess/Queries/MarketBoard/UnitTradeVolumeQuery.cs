using System;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class TradeVolumeQuery
{
    public int WorldId { get; init; }

    public int ItemId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}
