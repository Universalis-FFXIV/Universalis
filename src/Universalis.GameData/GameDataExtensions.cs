using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Universalis.GameData
{
    public static class GameDataExtensions
    {
        public static void AddGameData(this IServiceCollection sc, IConfiguration config)
        {
            sc.AddSingleton<IGameDataProvider>(_ => new LuminaGameDataProvider(config["GameData:SqPack"]));
        }
    }
}