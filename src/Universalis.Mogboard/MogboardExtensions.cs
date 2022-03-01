using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Universalis.Mogboard
{
    public static class MogboardExtensions
    {
        public static void AddMogboard(this IServiceCollection sc, IConfiguration config)
        {
            sc.AddSingleton(new UserListService(config["MogboardDatabaseUsername"], config["MogboardDatabasePassword"], config["MogboardDatabase"]));
        }
    }
}