using System.Collections.Generic;
using Universalis.Common.Caching;

namespace Universalis.Entities.MarketBoard;

public class CurrentlyShown : ICopyable
{
    public uint WorldId { get; }
    
    public uint ItemId { get; }

    public long LastUploadTimeUnixMilliseconds { get; }
    
    public string UploadSource { get; }

    public List<Listing> Listings { get; }

    public CurrentlyShown(uint worldId, uint itemId, long lastUploadTimeUnixMilliseconds, string uploadSource,
        List<Listing> listings)
    {
        WorldId = worldId;
        ItemId = itemId;
        LastUploadTimeUnixMilliseconds = lastUploadTimeUnixMilliseconds;
        UploadSource = uploadSource;
        Listings = listings;
    }

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }
}