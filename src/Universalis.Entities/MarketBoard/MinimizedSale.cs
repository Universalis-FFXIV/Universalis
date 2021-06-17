namespace Universalis.Entities.MarketBoard
{
    public class MinimizedSale
    {
        public bool Hq { get; set; }

        public uint PricePerUnit { get; set; }

        public uint Quantity { get; set; }

        public uint Timestamp { get; set; }

        public string UploaderIdHash { get; set; }
    }
}