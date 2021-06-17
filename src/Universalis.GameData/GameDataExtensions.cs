using Microsoft.Extensions.DependencyInjection;

namespace Universalis.GameData
{
    public static class GameDataExtensions
    {
        public static void AddGameData(this IServiceCollection sc)
        {
            sc.AddSingleton<IGameDataProvider, GameDataProvider>();
        }
    }
}