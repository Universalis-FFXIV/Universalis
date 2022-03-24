using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

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

            sc.AddSingleton<IMogboardTable<User, UserId>>(new UsersService(username, password, database, port));
            sc.AddSingleton<IMogboardTable<UserList, UserListId>>(new UserListsService(username, password, database, port));
            sc.AddSingleton<IMogboardTable<UserRetainer, UserRetainerId>>(new UserRetainersService(username, password, database, port));
            sc.AddSingleton<IMogboardTable<UserCharacter, UserCharacterId>>(new UserCharactersService(username, password, database, port));
            sc.AddSingleton<IMogboardTable<UserSession, UserSessionId>>(new UserSessionsService(username, password, database, port));
            sc.AddSingleton<IMogboardTable<UserAlert, UserAlertId>>(new UserAlertsService(username, password, database, port));
            sc.AddSingleton<IMogboardTable<UserAlertEvent, UserAlertEventId>>(new UserAlertEventsService(username, password, database, port));
            sc.AddSingleton<IMogboardTable<UserReport, UserReportId>>(new UserReportsService(username, password, database, port));
        }
    }
}