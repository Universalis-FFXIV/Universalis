namespace Universalis.DbAccess.Queries.MarketBoard;

public class HistoryManyQuery
{
    public int[] WorldIds { get; init; }

    public int ItemId { get; init; }

    public int? Count { get; init; }
}