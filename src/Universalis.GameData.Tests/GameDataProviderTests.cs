using Xunit;

#pragma warning disable xUnit1008 // Test data attribute should only be used on a Theory
namespace Universalis.GameData.Tests;

public class GameDataProviderTests
{
    private const string SqPack = @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack";
        
    public void Provider_Must_Load()
    {
        ServiceUtils.CreateGameDataProvider(SqPack);
    }
        
    [InlineData(44, "Anima")]
    [InlineData(74, "Coeurl")]
    [InlineData(82, "Mandragora")]
    public void AvailableWorlds_Should_Return_Correct_Ids(int worldId, string expectedWorldName)
    {
        var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
        var actualWorldName = gameData.AvailableWorlds()[worldId];
        Assert.Equal(expectedWorldName, actualWorldName);
    }
        
    [InlineData("Anima", 44)]
    [InlineData("Coeurl", 74)]
    [InlineData("Mandragora", 82)]
    public void AvailableWorldsReversed_Should_Return_Correct_Names(string worldName, int expectedWorldId)
    {
        var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
        var actualWorldId = gameData.AvailableWorldsReversed()[worldName];
        Assert.Equal(expectedWorldId, actualWorldId);
    }
        
    [InlineData(44, true)]
    [InlineData(74, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void AvailableWorldIds_Should_Only_Contain_Real_World_Ids(int worldId, bool expectedToContain)
    {
        var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
        var worldIds = gameData.AvailableWorldIds();
        var actuallyContains = worldIds.Contains(worldId);
        Assert.Equal(expectedToContain, actuallyContains);
    }
        
    [InlineData(26165, true)]
    [InlineData(30759, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void MarketableItemIds_Should_Only_Contain_Real_Item_Ids(int itemId, bool expectedToContain)
    {
        var gameData = ServiceUtils.CreateGameDataProvider(SqPack);
        var worldIds = gameData.MarketableItemIds();
        var actuallyContains = worldIds.Contains(itemId);
        Assert.Equal(expectedToContain, actuallyContains);
    }
}
#pragma warning restore xUnit1008 // Test data attribute should only be used on a Theory
