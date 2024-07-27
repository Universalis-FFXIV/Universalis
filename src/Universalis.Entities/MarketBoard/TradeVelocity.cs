namespace Universalis.Entities.MarketBoard;

public record TradeVelocity
{
    public required long Quantity { get; init; }
    public required long SumSales { get; init; }
    public required double AvgSalesPerDay { get; init; }
}
