using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Universalis.GameData
{
    public static class GameDataExtensions
    {
        public static void AddGameData(this IServiceCollection sc, IConfiguration config)
        {
            sc.AddSingleton<IGameDataProvider>(_ => new RobustGameDataProvider(new RobustGameDataProviderParams
            {
                Http = new HttpClient(),
                SqPack = config["GameData:SqPack"],
            }));
        }
    }
}