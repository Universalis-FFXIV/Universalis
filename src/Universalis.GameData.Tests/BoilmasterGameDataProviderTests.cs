using Xunit;

namespace Universalis.GameData.Tests;

public class BoilmasterGameDataProviderTests
{
    private static readonly IGameDataProvider GameData = ServiceUtils.CreateBoilmasterGameDataProvider();

    [InlineData(44, "Anima")]
    [InlineData(74, "Coeurl")]
    [InlineData(82, "Mandragora")]
    [InlineData(410, "Rafflesia")]
    [Theory]
    public void AvailableWorlds_Should_Return_Correct_Ids(int worldId, string expectedWorldName)
    {
        var actualWorldName = GameData.AvailableWorlds()[worldId];
        Assert.Equal(expectedWorldName, actualWorldName);
    }

    [InlineData("Anima", 44)]
    [InlineData("Coeurl", 74)]
    [InlineData("Mandragora", 82)]
    [InlineData("Rafflesia", 410)]
    [Theory]
    public void AvailableWorldsReversed_Should_Return_Correct_Names(string worldName, int expectedWorldId)
    {
        var actualWorldId = GameData.AvailableWorldsReversed()[worldName];
        Assert.Equal(expectedWorldId, actualWorldId);
    }

    [InlineData(44, true)]
    [InlineData(74, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [Theory]
    public void AvailableWorldIds_Should_Only_Contain_Real_World_Ids(int worldId, bool expectedToContain)
    {
        var worldIds = GameData.AvailableWorldIds();
        var actuallyContains = worldIds.Contains(worldId);
        Assert.Equal(expectedToContain, actuallyContains);
    }

    [InlineData(26165, true)]
    [InlineData(30759, true)]
    [InlineData(47979, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [Theory]
    public void MarketableItemIds_Should_Only_Contain_Real_Item_Ids(int itemId, bool expectedToContain)
    {
        var worldIds = GameData.MarketableItemIds();
        var actuallyContains = worldIds.Contains(itemId);
        Assert.Equal(expectedToContain, actuallyContains);
    }

    [InlineData(26165, 1)]
    [InlineData(30759, 1)]
    [InlineData(38953, 1)] // 6.3 items
    [InlineData(38954, 1)] // 6.3 items
    [InlineData(4551, 999)] // Stackable Item
    [Theory]
    public void MarketableItemStackSizes_Should_Only_Contain_Real_Stack_Sizes(int itemId, int expectedStackSize)
    {
        var worldIds = GameData.MarketableItemStackSizes();
        var actuallyContains = worldIds.TryGetValue(itemId, out int stackSizeValue);
        Assert.True(actuallyContains);
        Assert.Equal(expectedStackSize, stackSizeValue);
    }
}