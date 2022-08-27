using System.Collections.Generic;
using System.Linq;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Caching;

public class CachedCurrentlyShownData : ICopyable
{
    public uint ItemId { get; set; }

    public uint WorldId { get; set; }

    public long LastUploadTimeUnixMilliseconds { get; set; }

    public List<ListingView> Listings { get; set; }

    public List<SaleView> RecentHistory { get; set; }

    public ICopyable Clone()
    {
        var copy = (CachedCurrentlyShownData)MemberwiseClone();
        copy.Listings = Listings.ToList();
        copy.RecentHistory = RecentHistory.ToList();
        return copy;
    }
}