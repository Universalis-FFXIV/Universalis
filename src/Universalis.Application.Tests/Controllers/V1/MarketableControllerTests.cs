using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1
{
    public class MarketableControllerTests
    {
        [Fact]
        public void Controller_Get_Succeeds()
        {
            var gameData = new MockGameDataProvider();
            var controller = new MarketableController(gameData);
            var result = controller.Get();
            Assert.Equal(gameData.MarketableItemIds(), result);
        }
    }
}