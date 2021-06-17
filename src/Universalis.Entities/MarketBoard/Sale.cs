namespace Universalis.Entities.MarketBoard
{
    public class Sale
    {
        public bool Hq { get; set; }

        public uint PricePerUnit { get; set; }

        public uint Quantity { get; set; }

        public string BuyerName { get; set; }

        public uint Timestamp { get; set; }

        public bool OnMannequin { get; set; }
    }
}