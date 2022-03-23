using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Universalis.Mogboard
{
    public static class MogboardExtensions
    {
        public static void AddMogboard(this IServiceCollection sc, IConfiguration config)
        {
            var username = config["MogboardDatabaseUsername"];
            var password = config["MogboardDatabasePassword"];
            var database = config["MogboardDatabase"];
            if (!int.TryParse(config["MogboardPort"], out var port))
            {
                Console.Error.WriteLine("Failed to parse mogboard database port: {0}", config["MogboardPort"]);
                return;
            }

            sc.AddSingleton(new UserService(username, password, database, port));
            sc.AddSingleton(new UserListService(username, password, database, port));
        }
    }
}