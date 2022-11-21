using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Migrations;
using Universalis.DbAccess.Uploads;

namespace Universalis.DbAccess;

public static class DbAccessExtensions
{
    public static void AddDbAccessServices(this IServiceCollection sc, IConfiguration configuration)
    {
        var redisCacheConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_REDIS_CACHE_CONNECTION") ??
                                    configuration["RedisCacheConnectionString"];
        var redisConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_REDIS_CONNECTION") ??
                                    configuration["RedisConnectionString"];
        var postgresConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_POSTGRES_CONNECTION") ??
                                       configuration["PostgresConnectionString"];
        var memcachedConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_MEMCACHED_CONNECTION") ??
                                       configuration["MemcachedConnectionString"];

        sc.AddSingleton<ICacheRedisMultiplexer>(_ => new WrappedRedisMultiplexer(ConnectionMultiplexer.Connect(redisCacheConnectionString)));
        sc.AddSingleton<IPersistentRedisMultiplexer>(_ => new WrappedRedisMultiplexer(ConnectionMultiplexer.Connect(redisConnectionString)));

        sc.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(postgresConnectionString)
                .ScanIn(typeof(CreateMarketItemTable).Assembly).For.All())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        sc.AddSingleton<IWorldItemUploadStore, WorldItemUploadStore>();
        sc.AddSingleton<IMostRecentlyUpdatedDbAccess, MostRecentlyUpdatedDbAccess>();

        sc.AddSingleton<ICurrentlyShownStore, CurrentlyShownStore>();
        sc.AddSingleton<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();

        sc.AddSingleton<IMarketItemStore, MarketItemStore>(sc => new MarketItemStore(postgresConnectionString, sc.GetRequiredService<ICacheRedisMultiplexer>(), sc.GetRequiredService<ILogger<MarketItemStore>>()));
        sc.AddSingleton<ISaleStore, SaleStore>(sc => new SaleStore(postgresConnectionString, sc.GetRequiredService<ICacheRedisMultiplexer>(), sc.GetRequiredService<ILogger<SaleStore>>()));
        sc.AddSingleton<IHistoryDbAccess, HistoryDbAccess>();

        sc.AddSingleton<ISaleStatisticsDbAccess, SaleStatisticsDbAccess>();

        sc.AddSingleton<ICharacterStore, CharacterStore>(_ => new CharacterStore(postgresConnectionString));
        sc.AddSingleton<ICharacterDbAccess, CharacterDbAccess>();

        sc.AddSingleton<IFlaggedUploaderStore, FlaggedUploaderStore>(_ => new FlaggedUploaderStore(postgresConnectionString));
        sc.AddSingleton<IFlaggedUploaderDbAccess, FlaggedUploaderDbAccess>();

        sc.AddSingleton<ITaxRatesStore, TaxRatesStore>();
        sc.AddSingleton<ITaxRatesDbAccess, TaxRatesDbAccess>();

        sc.AddSingleton<IWorldUploadCountStore, WorldUploadCountStore>();
        sc.AddSingleton<IWorldUploadCountDbAccess, WorldUploadCountDbAccess>();

        sc.AddSingleton<IDailyUploadCountStore, DailyUploadCountStore>();
        sc.AddSingleton<IUploadCountHistoryDbAccess, UploadCountHistoryDbAccess>();

        sc.AddSingleton<IApiKeyStore, ApiKeyStore>(_ => new ApiKeyStore(postgresConnectionString));
        sc.AddSingleton<ISourceUploadCountStore, TrustedSourceUploadCountStore>();
        sc.AddSingleton<ITrustedSourceDbAccess, TrustedSourceDbAccess>();

        sc.AddSingleton<IRecentlyUpdatedItemsStore, RecentlyUpdatedItemsStore>();
        sc.AddSingleton<IRecentlyUpdatedItemsDbAccess, RecentlyUpdatedItemsDbAccess>();
    }
}