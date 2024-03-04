using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using Cassandra.Metrics;
using FluentMigrator.Runner;
using Npgsql;
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
                .ScanIn(typeof(DbAccessExtensions).Assembly).For.All())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
        dataSourceBuilder.UseJsonNet();
        sc.AddSingleton(dataSourceBuilder.Build());

        Diagnostics.CassandraPerformanceCountersEnabled = true;

        MappingConfiguration.Global.Define<ObjectMappings>();

        // Notes on query idempotence and speculative execution: https://docs.datastax.com/en/developer/csharp-driver/3.20/features/speculative-retries/#query-idempotence
        var scyllaCluster = Cluster.Builder()
            .AddContactPoints(scyllaConnectionString.Split(','))
            .WithSpeculativeExecutionPolicy(new ConstantSpeculativeExecutionPolicy(200, 3))
            .WithQueryOptions(new QueryOptions()
                .SetDefaultIdempotence(true)
                .SetPageSize(scyllaPageSize))
            .WithMetrics(new PrometheusDataStaxMetricsProvider(), new DriverMetricsOptions()
                .SetEnabledNodeMetrics(NodeMetric.AllNodeMetrics)
                .SetEnabledSessionMetrics(SessionMetric.AllSessionMetrics))
            .Build();
        sc.AddSingleton<ICluster>(scyllaCluster);

        var cacheOptions = ConfigurationOptions.Parse(redisCacheConnectionString);
        var cache = ConnectionMultiplexer.Connect(cacheOptions);
        var dbOptions = ConfigurationOptions.Parse(redisConnectionString);
        var db = ConnectionMultiplexer.Connect(dbOptions);
        sc.AddSingleton<ICacheRedisMultiplexer>(_ => new WrappedRedisMultiplexer(cache));
        sc.AddSingleton<IPersistentRedisMultiplexer>(_ => new WrappedRedisMultiplexer(db));

        sc.AddSingleton<IUploadLogStore, UploadLogStore>();
        sc.AddSingleton<IUploadLogDbAccess, UploadLogDbAccess>();

        sc.AddSingleton<IListingStore, ListingStore>();

        sc.AddSingleton<IWorldItemUploadStore, WorldItemUploadStore>();
        sc.AddSingleton<IMostRecentlyUpdatedDbAccess, MostRecentlyUpdatedDbAccess>();

        sc.AddSingleton<ICurrentlyShownStore, CurrentlyShownStore>();
        sc.AddSingleton<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();

        sc.AddSingleton<IMarketItemStore, MarketItemStore>();
        sc.AddSingleton<ISaleStore, SaleStore>();
        sc.AddSingleton<IHistoryDbAccess, HistoryDbAccess>();

        sc.AddSingleton<ISaleStatisticsDbAccess, SaleStatisticsDbAccess>();

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
    }
}