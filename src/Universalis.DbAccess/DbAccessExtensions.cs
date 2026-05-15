using System;
using Cassandra;
using Cassandra.Mapping;
using Cassandra.Metrics;
using EasyCaching.InMemory;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using StackExchange.Redis;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Metrics;
using Universalis.DbAccess.Uploads;

namespace Universalis.DbAccess;

public static class DbAccessExtensions
{
    public static void AddDbAccessServices(this IServiceCollection sc, IConfiguration configuration)
    {
        var redisCacheConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_REDIS_CACHE_CONNECTION") ??
                                         configuration["RedisCacheConnectionString"] ??
                                         throw new InvalidOperationException(
                                             "Redis cache connection string not provided.");
        var redisConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_REDIS_CONNECTION") ??
                                    configuration["RedisConnectionString"] ??
                                    throw new InvalidOperationException(
                                        "Redis primary connection string not provided.");
        var scyllaConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_SCYLLA_CONNECTION") ??
                                     configuration["ScyllaConnectionString"] ??
                                     throw new InvalidOperationException(
                                         "ScyllaDB connection string not provided.");
        var postgresConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_POSTGRES_CONNECTION") ??
                                       configuration["PostgresConnectionString"] ??
                                       throw new InvalidOperationException(
                                           "PostgreSQL connection string not provided.");

        var scyllaPageSize = int.Parse(Environment.GetEnvironmentVariable("UNIVERSALIS_SCYLLA_PAGE_SIZE") ?? "100");

        // An optional separate connection string so that different settings can be used
        // during migrations versus under load. Mostly so that multiplexing can be enabled
        // in Npgsql.
        var fluentMigratorConnectionString =
            Environment.GetEnvironmentVariable("UNIVERSALIS_FLUENTMIGRATOR_CONNECTION") ??
            configuration["FluentMigratorConnectionString"] ??
            postgresConnectionString;

        sc.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(fluentMigratorConnectionString)
                // Npgsql's default 30s command timeout is too short for DDL that
                // takes ACCESS EXCLUSIVE on hot tables (e.g. PK rebuilds): the
                // build itself plus lock-acquisition wait under live traffic can
                // exceed the default. Allow generous headroom so migrations
                // queue rather than crash the application on startup.
                .WithGlobalCommandTimeout(TimeSpan.FromMinutes(30))
                .ScanIn(typeof(DbAccessExtensions).Assembly).For.All());

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
        dataSourceBuilder.UseJsonNet();
        sc.AddSingleton(dataSourceBuilder
            .EnableDynamicJson()
            .Build());

        Diagnostics.CassandraPerformanceCountersEnabled = true;

        MappingConfiguration.Global.Define<ObjectMappings>();

        // Notes on query idempotence and speculative execution: https://docs.datastax.com/en/developer/csharp-driver/3.20/features/speculative-retries/#query-idempotence
        var scyllaCluster = Cluster.Builder()
            .AddContactPoints(scyllaConnectionString.Split(','))
            .WithPoolingOptions(PoolingOptions.Create()
                .SetMaxRequestsPerConnection(3000))
            .WithSpeculativeExecutionPolicy(new ConstantSpeculativeExecutionPolicy(400, 3))
            .WithQueryTimeout(5000)
            .WithQueryOptions(new QueryOptions()
                .SetDefaultIdempotence(true)
                .SetPageSize(scyllaPageSize))
            .WithMetrics(new PrometheusDataStaxMetricsProvider(), new DriverMetricsOptions()
                .SetEnabledNodeMetrics(NodeMetric.AllNodeMetrics)
                .SetEnabledSessionMetrics(SessionMetric.AllSessionMetrics))
            .Build();
        sc.AddSingleton<ICluster>(scyllaCluster);

        sc.AddEasyCaching(options =>
        {
            options.UseInMemory(config =>
            {
                config.DBConfig = new InMemoryCachingOptions
                {
                    ExpirationScanFrequency = 60,
                    SizeLimit = 100000,
                    EnableReadDeepClone = false,
                    EnableWriteDeepClone = false,
                };

                config.MaxRdSecond = 120;
                config.EnableLogging = false;
                config.LockMs = 5000;
                config.SleepMs = 300;
            });
        });

        var cacheOptions = ConfigurationOptions.Parse(redisCacheConnectionString);
        var cache1 = ConnectionMultiplexer.Connect(cacheOptions);
        var cache2 = ConnectionMultiplexer.Connect(cacheOptions);
        var cache3 = ConnectionMultiplexer.Connect(cacheOptions);
        var dbOptions = ConfigurationOptions.Parse(redisConnectionString);
        var db = ConnectionMultiplexer.Connect(dbOptions);
        sc.AddSingleton<ICacheRedisMultiplexer>(_ => new WrappedRedisMultiplexer(cache1, cache2, cache3));
        sc.AddSingleton<IPersistentRedisMultiplexer>(_ => new WrappedRedisMultiplexer(db));

        sc.AddSingleton<IUploadLogStore, UploadLogStore>();

        // UploadLogDbAccess is a batching wrapper that doubles as a hosted service:
        // the request path calls LogAction (channel write only), and the hosted-service
        // background loop drains the channel into the store in batches.
        sc.AddSingleton<UploadLogDbAccess>();
        sc.AddSingleton<IUploadLogDbAccess>(sp => sp.GetRequiredService<UploadLogDbAccess>());
        sc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<UploadLogDbAccess>());

        sc.AddSingleton<IListingStore, ListingStore>();

        sc.AddSingleton<IWorldItemUploadStore, WorldItemUploadStore>();
        sc.AddSingleton<IMostRecentlyUpdatedDbAccess, MostRecentlyUpdatedDbAccess>();

        sc.AddSingleton<ICurrentlyShownStore, CurrentlyShownStore>();
        sc.AddSingleton<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();

        sc.AddSingleton<IMarketItemStore, MarketItemStore>();
        sc.AddSingleton<ISaleStore, SaleStore>();
        sc.AddSingleton<IHistoryDbAccess, HistoryDbAccess>();

        sc.AddSingleton<ICharacterStore, CharacterStore>();
        sc.AddSingleton<ICharacterDbAccess, CharacterDbAccess>();

        sc.AddSingleton<IFlaggedUploaderStore, FlaggedUploaderStore>();
        sc.AddSingleton<IFlaggedUploaderDbAccess, FlaggedUploaderDbAccess>();

        sc.AddSingleton<ITaxRatesStore, TaxRatesStore>();
        sc.AddSingleton<ITaxRatesDbAccess, TaxRatesDbAccess>();

        sc.AddSingleton<IWorldUploadCountStore, WorldUploadCountStore>();
        sc.AddSingleton<IWorldUploadCountDbAccess, WorldUploadCountDbAccess>();

        sc.AddSingleton<IDailyUploadCountStore, DailyUploadCountStore>();
        sc.AddSingleton<IUploadCountHistoryDbAccess, UploadCountHistoryDbAccess>();

        sc.AddSingleton<IApiKeyStore, ApiKeyStore>();
        sc.AddSingleton<ISourceUploadCountStore, TrustedSourceUploadCountStore>();
        sc.AddSingleton<ITrustedSourceDbAccess, TrustedSourceDbAccess>();

        sc.AddSingleton<IRecentlyUpdatedItemsStore, RecentlyUpdatedItemsStore>();
        sc.AddSingleton<IRecentlyUpdatedItemsDbAccess, RecentlyUpdatedItemsDbAccess>();

        sc.AddSingleton<IAggregatedMarketBoardDataDbAccess, AggregatedMarketBoardDataDbAccess>();
    }
}