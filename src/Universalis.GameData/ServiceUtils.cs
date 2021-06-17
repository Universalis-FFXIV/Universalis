namespace Universalis.GameData
{
    public static class ServiceUtils
    {
        public static IGameDataProvider CreateGameDataProvider(string sqpack)
        {
            var lumina = new Lumina.GameData(sqpack);
            return new GameDataProvider(lumina);
        }
    }
}