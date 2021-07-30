using Xunit;

#pragma warning disable xUnit1004 // Test methods should not be skipped
namespace Universalis.GameData.Tests
{
    public class GameDataProviderTests
    {
        private const string SqPack = @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack";

        private const string Skip =
#if !DEBUG
            "This test is only run in the Debug configuration for build automation purposes.";
#else
            null;
#endif

        [Fact(Skip = Skip)]
        public void Provider_Must_Load()
        {
            ServiceUtils.CreateGameDataProvider(SqPack);
        }

        [Theory(Skip = Skip)]
        [InlineData(44, "Anima")]
        [InlineData(74, "Coeurl")]
        [InlineData(82, "Mandragora")]
        public void AvailableWorlds_Should_Return_Correct_Ids(uint worldId, string expectedWorldName)
        {
            var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
            var actualWorldName = gameData.AvailableWorlds()[worldId];
            Assert.Equal(expectedWorldName, actualWorldName);
        }

        [Theory(Skip = Skip)]
        [InlineData("Anima", 44)]
        [InlineData("Coeurl", 74)]
        [InlineData("Mandragora", 82)]
        public void AvailableWorldsReversed_Should_Return_Correct_Names(string worldName, uint expectedWorldId)
        {
            var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
            var actualWorldId = gameData.AvailableWorldsReversed()[worldName];
            Assert.Equal(expectedWorldId, actualWorldId);
        }

        [Theory(Skip = Skip)]
        [InlineData(44, true)]
        [InlineData(74, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void AvailableWorldIds_Should_Only_Contain_Real_World_Ids(uint worldId, bool expectedToContain)
        {
            var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
            var worldIds = gameData.AvailableWorldIds();
            var actuallyContains = worldIds.Contains(worldId);
            Assert.Equal(expectedToContain, actuallyContains);
        }

        [Theory(Skip = Skip)]
        [InlineData(26165, true)]
        [InlineData(30759, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void MarketableItemIds_Should_Only_Contain_Real_Item_Ids(uint itemId, bool expectedToContain)
        {
            var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
            var worldIds = gameData.MarketableItemIds();
            var actuallyContains = worldIds.Contains(itemId);
            Assert.Equal(expectedToContain, actuallyContains);
        }
    }
}
#pragma warning restore xUnit1004 // Test methods should not be skipped
