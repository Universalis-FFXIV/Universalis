using System.Collections.Generic;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Caching;

public class CachedCurrentlyShownData
{
    public uint ItemId { get; set; }
    
    public uint WorldId { get; set; }

    public long LastUploadTimeUnixMilliseconds { get; set; }
    
    public List<ListingView> Listings { get; set; }
    
    public List<SaleView> RecentHistory { get; set; }
}