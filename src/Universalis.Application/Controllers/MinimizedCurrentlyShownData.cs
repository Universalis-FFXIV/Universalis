using System.Collections.Generic;
using Universalis.Application.Views.V1;

namespace Universalis.Application.Controllers;

public class MinimizedCurrentlyShownData
{
    public uint ItemId { get; set; }
    
    public uint WorldId { get; set; }

    public long LastUploadTimeUnixMilliseconds { get; set; }
    
    public List<ListingView> Listings { get; set; }
    
    public List<SaleView> RecentHistory { get; set; }
}