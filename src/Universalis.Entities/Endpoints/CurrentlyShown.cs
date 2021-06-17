using System.Collections.Generic;
using Universalis.Entities.MarketBoard;

namespace Universalis.Entities.Endpoints
{
    public class CurrentlyShown
    {
        public byte UploadApplication { get; set; }

        public string UploaderIdHash { get; set; }

        public List<Listing> Listings { get; set; }

        public List<Sale> RecentHistory { get; set; }
    }
}