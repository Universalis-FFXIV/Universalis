namespace Universalis.GameData
{
    public static class ServiceUtils
    {
        public static IGameDataProvider CreateGameDataProvider(string sqpack)
        {
            return new LuminaGameDataProvider(sqpack);
        }
    }
}