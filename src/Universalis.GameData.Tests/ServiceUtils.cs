using System.Net.Http;
using Universalis.Tests;

namespace Universalis.GameData.Tests;

public static class ServiceUtils
{
    public static IGameDataProvider CreateGameDataProvider(string sqpack)
    {
        return new DynamicGameDataProvider(new DynamicGameDataProviderOptions
        {
            Http = new HttpClient(),
            SqPack = sqpack,
        }, new LogFixture<DynamicGameDataProvider>());
    }
}