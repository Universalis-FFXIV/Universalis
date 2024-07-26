namespace Universalis.Entities.MarketBoard;

public record TradeVelocity(
    long Quantity,
    long SumSales,
    double AvgSalesPerDay
);