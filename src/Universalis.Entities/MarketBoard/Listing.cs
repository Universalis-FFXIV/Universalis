using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard
{
    public class Listing
    {
        public string ListingId { get; set; }

        public bool Hq { get; set; }

        public List<Materia> Materia { get; set; }

        public uint PricePerUnit { get; set; }

        public uint Quantity { get; set; }

        public byte Dye { get; set; }

        public bool IsCrafted { get; set; }

        public string CreatorId { get; set; }

        public string CreatorName { get; set; }

        public uint LastReviewTime { get; set; }

        public string RetainerId { get; set; }

        public string RetainerName { get; set; }

        public byte RetainerCity { get; set; }

        public uint WorldId { get; set; }
    }
}