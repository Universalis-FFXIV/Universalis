using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;
using Universalis.Mogboard.Identity;

namespace Universalis.Mogboard;

public static class MogboardExtensions
{
    public static void AddMogboard(this IServiceCollection sc, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_MOGBOARD_CONNECTION") ??
                               configuration["MogboardConnectionString"] ??
                               throw new InvalidOperationException("Mogboard connection string not provided.");

        sc.AddSingleton<IMogboardTable<User, UserId>>(new UsersService(connectionString));
        sc.AddSingleton<IMogboardTable<UserList, UserListId>>(new UserListsService(connectionString));
        sc.AddSingleton<IMogboardTable<UserRetainer, UserRetainerId>>(new UserRetainersService(connectionString));
        sc.AddSingleton<IMogboardTable<UserCharacter, UserCharacterId>>(new UserCharactersService(connectionString));
        sc.AddSingleton<IMogboardSessionTable>(new UserSessionsService(connectionString));
        sc.AddSingleton<IMogboardTable<UserAlert, UserAlertId>>(new UserAlertsService(connectionString));
        sc.AddSingleton<IMogboardTable<UserAlertEvent, UserAlertEventId>>(new UserAlertEventsService(connectionString));
        sc.AddSingleton<IMogboardTable<UserReport, UserReportId>>(new UserReportsService(connectionString));
        sc.AddSingleton<IMogboardAuthenticationService, MogboardAuthenticationService>();
    }
}