using System;

namespace Universalis.Entities.MarketBoard;

public record RecentSale
{
    public required int UnitPrice { get; init; }
    public required DateTime SaleTime { get; init; }
    public required int WorldId { get; init; }
}
