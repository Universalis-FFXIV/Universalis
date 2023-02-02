using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

public static class MogboardExtensions
{
    public static void AddMogboard(this IServiceCollection sc, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_MOGBOARD_CONNECTION") ??
                               configuration["MogboardConnectionString"] ??
                               throw new InvalidOperationException("Mogboard connection string not provided.");

        sc.AddSingleton<IMogboardTable<UserList, UserListId>>(new UserListsService(connectionString));
    }
}