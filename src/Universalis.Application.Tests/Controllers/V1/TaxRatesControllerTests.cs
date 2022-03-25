using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views;
using Universalis.Application.Views.V1;
using Universalis.DbAccess.Tests;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1;

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

        var expectedTaxRates = SeedDataGenerator.MakeTaxRates(74);
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

        await dbAccess.Create(SeedDataGenerator.MakeTaxRates(74));

        var result = await controller.Get("crystal");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Get_FailsWithInvalidWorld()
    {
        var gameData = new MockGameDataProvider();
        var dbAccess = new MockTaxRatesDbAccess();
        var controller = new TaxRatesController(gameData, dbAccess);

        await dbAccess.Create(SeedDataGenerator.MakeTaxRates(74));

        var result = await controller.Get("50");
        Assert.IsType<NotFoundResult>(result);
    }
}