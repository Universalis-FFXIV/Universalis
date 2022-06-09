using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class CurrentlyShownSimple
{
    public uint WorldId { get; }
    
    public uint ItemId { get; }

    public long LastUploadTimeUnixMilliseconds { get; }
    
    public string UploadSource { get; }

    public List<Listing> Listings { get; }

    public List<Sale> Sales { get; }

    public CurrentlyShownSimple(uint worldId, uint itemId, long uploadTimeUnixMs, string source,
        List<Listing> listings, List<Sale> sales)
    {
        WorldId = worldId;
        ItemId = itemId;
        LastUploadTimeUnixMilliseconds = uploadTimeUnixMs;
        UploadSource = source;
        Listings = listings;
        Sales = sales;
    }
}