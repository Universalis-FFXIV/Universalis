namespace Universalis.DbAccess.Queries.MarketBoard;

public class HistoryQuery
{
    public int WorldId { get; init; }

    public int ItemId { get; init; }
    
    public int? Count { get; init; }
}