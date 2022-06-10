namespace Universalis.DbAccess.Queries.MarketBoard;

public class HistoryQuery
{
    public uint WorldId { get; init; }

    public uint ItemId { get; init; }
    
    public int? Count { get; init; }
}