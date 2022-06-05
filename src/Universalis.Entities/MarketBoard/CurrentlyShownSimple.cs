using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class CurrentlyShownSimple
{
    public uint WorldId { get; }
    
    public uint ItemId { get; }

    public long LastUploadTimeUnixMilliseconds { get; }
    
    public string UploadSource { get; }

    public List<ListingSimple> Listings { get; }

    public List<SaleSimple> Sales { get; }

    public CurrentlyShownSimple(uint worldId, uint itemId, long uploadTimeUnixMs, string source,
        List<ListingSimple> listings, List<SaleSimple> sales)
    {
        WorldId = worldId;
        ItemId = itemId;
        LastUploadTimeUnixMilliseconds = uploadTimeUnixMs;
        UploadSource = source;
        Listings = listings;
        Sales = sales;
    }
}