using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard
{
    public class CurrentlyShown
    {
        public byte UploadApplication { get; set; }

        public string UploaderIdHash { get; set; }

        public uint ItemId { get; set; }

        public uint WorldId { get; set; }

        public List<Listing> Listings { get; set; }

        public List<Sale> RecentHistory { get; set; }
    }
}