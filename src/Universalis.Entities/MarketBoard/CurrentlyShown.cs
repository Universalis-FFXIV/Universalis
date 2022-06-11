using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class CurrentlyShown
{
    public uint WorldId { get; }
    
    public uint ItemId { get; }

    public long LastUploadTimeUnixMilliseconds { get; }
    
    public string UploadSource { get; }

    public List<Listing> Listings { get; }

    public CurrentlyShown(uint worldId, uint itemId, long uploadTimeUnixMs, string source,
        List<Listing> listings)
    {
        WorldId = worldId;
        ItemId = itemId;
        LastUploadTimeUnixMilliseconds = uploadTimeUnixMs;
        UploadSource = source;
        Listings = listings;
    }
}