namespace Universalis.Entities.MarketBoard;

public record MinListing(
    MinListing.Entry World,
    MinListing.Entry Dc,
    MinListing.Entry Region
)
{
    public record Entry(Price Nq, Price Hq);

    public record Price(int WorldId, int UnitPrice);
}
