using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Universalis.GameData;

public static class GameDataExtensions
{
    public static void AddGameData(this IServiceCollection sc, IConfiguration config)
    {
        sc.AddSingleton<IGameDataProvider>(services => new DynamicGameDataProvider(new DynamicGameDataProviderOptions
        {
            Http = new HttpClient(),
            SqPack = config["GameData:SqPack"],
        }, services.GetRequiredService<ILogger<DynamicGameDataProvider>>()));
    }
}