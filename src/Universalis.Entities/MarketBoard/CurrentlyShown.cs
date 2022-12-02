using System.Collections.Generic;
using Universalis.Common.Caching;

namespace Universalis.Entities.MarketBoard;

public class CurrentlyShown : ICopyable
{
    public int WorldId { get; init; }
    
    public int ItemId { get; init; }

    public long LastUploadTimeUnixMilliseconds { get; init; }

    public string UploadSource { get; init; } = "";

    public List<Listing> Listings { get; init; } = new List<Listing>();

    public ICopyable Clone()
    {
        return (ICopyable)MemberwiseClone();
    }
}