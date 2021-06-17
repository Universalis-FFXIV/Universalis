using Microsoft.Extensions.DependencyInjection;

namespace Universalis.Alerts
{
    public static class AlertExtensions
    {
        public static void AddUserAlerts(this IServiceCollection sc)
        {
            sc.AddSingleton<IDiscordAlertsProvider, DiscordAlertsProvider>();
        }
    }
}