namespace Universalis.Entities.MarketBoard;

public class Sale
{
    public bool Hq { get; init; }

    public uint PricePerUnit { get; init; }

    public uint Quantity { get; init; }

    public string BuyerName { get; init; }

    public long TimestampUnixSeconds { get; init; }
}