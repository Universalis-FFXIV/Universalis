using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views;
using Universalis.Entities.MarketBoard;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1
{
    public class TaxRatesControllerTests
    {
        [Theory]
        [InlineData("74")]
        [InlineData("Coeurl")]
        [InlineData("coEUrl")]
        public async Task Controller_Get_SucceedsWithWorld(string world)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockTaxRatesDbAccess();
            var controller = new TaxRatesController(gameData, dbAccess);

            var expectedTaxRates = GetTaxRatesSeed(74);
            await dbAccess.Create(expectedTaxRates);

            var result = await controller.Get(world);
            var taxRates = (TaxRatesView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Equal(expectedTaxRates.LimsaLominsa, taxRates.LimsaLominsa);
            Assert.Equal(expectedTaxRates.Gridania, taxRates.Gridania);
            Assert.Equal(expectedTaxRates.Uldah, taxRates.Uldah);
            Assert.Equal(expectedTaxRates.Ishgard, taxRates.Ishgard);
            Assert.Equal(expectedTaxRates.Kugane, taxRates.Kugane);
            Assert.Equal(expectedTaxRates.Crystarium, taxRates.Crystarium);
        }

        [Fact]
        public async Task Controller_Get_FailsWithDataCenter()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockTaxRatesDbAccess();
            var controller = new TaxRatesController(gameData, dbAccess);

            await dbAccess.Create(GetTaxRatesSeed(74));

            var result = await controller.Get("crystal");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Controller_Get_FailsWithInvalidWorld()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockTaxRatesDbAccess();
            var controller = new TaxRatesController(gameData, dbAccess);

            await dbAccess.Create(GetTaxRatesSeed(74));

            var result = await controller.Get("50");
            Assert.IsType<NotFoundResult>(result);
        }

        private static TaxRates GetTaxRatesSeed(uint worldId)
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