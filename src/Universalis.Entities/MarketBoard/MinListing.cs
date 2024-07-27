namespace Universalis.Entities.MarketBoard;

public record MinListing
{
    public Entry World { get; init; }
    public required Entry Dc { get; init; }
    public required Entry Region { get; init; }

    public record Entry(Price Nq, Price Hq);

    public record Price(int WorldId, int UnitPrice);
}
