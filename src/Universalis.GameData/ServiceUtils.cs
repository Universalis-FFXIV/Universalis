using System.Net.Http;

namespace Universalis.GameData;

public static class ServiceUtils
{
    public static IGameDataProvider CreateGameDataProvider(string sqpack)
    {
        return new RobustGameDataProvider(new RobustGameDataProviderParams
        {
            Http = new HttpClient(),
            SqPack = sqpack,
        });
    }
}