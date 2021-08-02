using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Universalis.GameData
{
    public static class GameDataExtensions
    {
        public static void AddGameData(this IServiceCollection sc, IConfiguration config)
        {
            Console.WriteLine("Loading from {0}", config["GameData:SqPack"]);
            sc.AddSingleton(_ => new Lumina.GameData(config["GameData:SqPack"]));
            sc.AddSingleton<IGameDataProvider, GameDataProvider>();
        }
    }
}