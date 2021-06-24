using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Universalis.Application.DbAccess;

namespace Universalis.Application
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Automatically migrate the database. In production, a backup should always be made before performing this operation.
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var applicationContexts = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(t => t.IsAssignableFrom(typeof(DbContext)));

                    foreach (var contextType in applicationContexts)
                    {
                        logger.LogInformation("Performing migration for {0}", contextType.Name);

                        var context = (DbContext)services.GetRequiredService(contextType);
                        context.Database.OpenConnection();
                        context.Database.Migrate();
                        context.SaveChanges();
                    }

                    logger.LogInformation("Migrations complete.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding and migrating the database.");
                }
            }

            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
