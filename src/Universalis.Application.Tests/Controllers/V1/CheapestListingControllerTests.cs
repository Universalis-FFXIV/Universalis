using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views;
using Universalis.DbAccess.Tests;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1
{
    public class CheapestListingControllerTests
    {
        [Fact]
        public async Task Controller_Get_Succeeds_SingleItem_World()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockCurrentlyShownDbAccess();
            var controller = new CheapestListingController(gameData, dbAccess);

            const uint itemId = 5333;
            var document = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
            await dbAccess.Create(document);

            var result = await controller.Get(itemId.ToString(), "74");
            var cheapest = (CheapestView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.NotNull(cheapest);
            Assert.NotEmpty(cheapest.Items);

            document.Listings.Sort((a, b) => (int)a.PricePerUnit - (int)b.PricePerUnit);
            
            Assert.NotNull(cheapest.Items[itemId]);
            Assert.Equal(document.Listings[0].Quantity, cheapest.Items[itemId].Quantity);

            Assert.Null(cheapest.Items[itemId].WorldId);
            Assert.Null(cheapest.Items[itemId].WorldName);
        }

        [Fact]
        public async Task Controller_Get_Succeeds_SingleItem_DataCenter()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockCurrentlyShownDbAccess();
            var controller = new CheapestListingController(gameData, dbAccess);

            const uint itemId = 5333;
            var document = SeedDataGenerator.MakeCurrentlyShown(74, itemId);
            await dbAccess.Create(document);

            var result = await controller.Get(itemId.ToString(), "Crystal");
            var cheapest = (CheapestView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.NotNull(cheapest);
            Assert.NotEmpty(cheapest.Items);

            document.Listings.Sort((a, b) => (int)a.PricePerUnit - (int)b.PricePerUnit);

            Assert.NotNull(cheapest.Items[itemId]);
            Assert.Equal(document.Listings[0].Quantity, cheapest.Items[itemId].Quantity);

            Assert.Equal(74U, cheapest.Items[itemId].WorldId);
            Assert.Equal("Coeurl", cheapest.Items[itemId].WorldName);
        }

        [Fact]
        public async Task Controller_Get_Succeeds_SingleItem_World_WhenNone()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockCurrentlyShownDbAccess();
            var controller = new CheapestListingController(gameData, dbAccess);

            const uint itemId = 5333;

            var result = await controller.Get(itemId.ToString(), "74");
            var cheapest = (CheapestView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.NotNull(cheapest);
            Assert.NotEmpty(cheapest.Items);
            
            Assert.Null(cheapest.Items[itemId]);
        }

        [Fact]
        public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNone()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockCurrentlyShownDbAccess();
            var controller = new CheapestListingController(gameData, dbAccess);

            const uint itemId = 5333;

            var result = await controller.Get(itemId.ToString(), "Crystal");
            var cheapest = (CheapestView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.NotNull(cheapest);
            Assert.NotEmpty(cheapest.Items);

            Assert.Null(cheapest.Items[itemId]);
        }

        [Fact]
        public async Task Controller_Get_Fails_SingleItem_WhenInvalid()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockCurrentlyShownDbAccess();
            var controller = new CheapestListingController(gameData, dbAccess);

            var result = await controller.Get("0", "74");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Controller_Get_Succeeds_MultiItem_WhenMultipleInvalid()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockCurrentlyShownDbAccess();
            var controller = new CheapestListingController(gameData, dbAccess);
            
            var result = await controller.Get("0,9999999", "74");
            var cheapest = (CheapestView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.NotNull(cheapest);
            Assert.NotEmpty(cheapest.Items);

            Assert.Null(cheapest.Items[0]);
            Assert.Null(cheapest.Items[9999999]);
        }
    }
}