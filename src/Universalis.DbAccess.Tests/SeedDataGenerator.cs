using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Tests
{
    public static class SeedDataGenerator
    {
        public static TaxRates GetTaxRates(uint worldId)
        {
            return new()
            {
                WorldId = worldId,
                UploaderIdHash = "",
                UploadApplicationName = "test runner",
                LimsaLominsa = 3,
                Gridania = 3,
                Uldah = 3,
                Ishgard = 0,
                Kugane = 0,
                Crystarium = 5,
            };
        }
    }
}