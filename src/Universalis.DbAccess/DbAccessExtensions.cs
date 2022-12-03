using Amazon.DynamoDBv2;
using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Linq;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess;

public static class DbAccessExtensions
{
    public static void AddDbAccessServices(this IServiceCollection sc, IConfiguration configuration)
    {
        var redisCacheConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_REDIS_CACHE_CONNECTION") ??
                                    configuration["RedisCacheConnectionString"];
        var redisConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_REDIS_CONNECTION") ??
                                    configuration["RedisConnectionString"];
        var scyllaConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_SCYLLA_CONNECTION") ??
                                    configuration["ScyllaConnectionString"];
        var ddbConnectionString = Environment.GetEnvironmentVariable("UNIVERSALIS_DYNAMODB_CONNECTION") ??
                                    configuration["AWS:ServiceURL"];

        var awsOptions = configuration.GetAWSOptions();
        sc.AddDefaultAWSOptions(awsOptions);

        // This needs to be initialized early so we can't use the extension methods that
        // configure things correctly automatically.
        // TODO: Move table initialization into middleware to avoid this
        var dynamoDb = new AmazonDynamoDBClient("None", "None", new AmazonDynamoDBConfig
        {
            LogMetrics = bool.TryParse(configuration.GetSection("AWS:LogMetrics").Value, out var logMetrics) && logMetrics,
            LogResponse = bool.TryParse(configuration.GetSection("AWS:LogResponse").Value, out var logResponse) && logResponse,
            ServiceURL = ddbConnectionString,
        });
        sc.AddSingleton<IAmazonDynamoDB>(dynamoDb);

        DynamoDBTableInitializer.InitializeTables(dynamoDb).GetAwaiter().GetResult();

        MappingConfiguration.Global.Define(
            new Map<FlaggedUploader>()
                .TableName("flagged_uploader")
                .PartitionKey(s => s.IdSha256)
                .Column(s => s.IdSha256, col => col.WithName("id_sha256")));

        MappingConfiguration.Global.Define(
            new Map<MarketItem>()
                .TableName("market_item")
                .PartitionKey(s => s.ItemId)
                .ClusteringKey(s => s.WorldId)
                .Column(s => s.ItemId, col => col.WithName("item_id"))
                .Column(s => s.WorldId, col => col.WithName("world_id"))
                .Column(s => s.LastUploadTime, col => col.WithName("last_upload_time")));

        MappingConfiguration.Global.Define(
            new Map<Sale>()
                .TableName("sale")
                .PartitionKey(s => s.Id)
                .ClusteringKey(s => s.SaleTime, SortOrder.Descending)
                .Column(s => s.Id, col => col.WithName("id"))
                .Column(s => s.SaleTime, col => col.WithName("sale_time"))
                .Column(s => s.ItemId, col => col.WithName("item_id"))
                .Column(s => s.WorldId, col => col.WithName("world_id"))
                .Column(s => s.BuyerName, col => col.WithName("buyer_name"))
                .Column(s => s.Hq, col => col.WithName("hq"))
                .Column(s => s.OnMannequin, col => col.WithName("on_mannequin"))
                .Column(s => s.Quantity, col => col.WithName("quantity"))
                .Column(s => s.PricePerUnit, col => col.WithName("unit_price"))
                .Column(s => s.UploaderIdHash, col => col.WithName("uploader_id")));

        var scyllaCluster = Cluster.Builder()
            .AddContactPoints(scyllaConnectionString.Split(','))
            .Build();
        sc.AddSingleton<ICluster>(scyllaCluster);

        var cacheOptions = ConfigurationOptions.Parse(redisCacheConnectionString);
        var cache = Enumerable.Range(0, 3).Select(_ => ConnectionMultiplexer.Connect(cacheOptions)).ToArray();
        var dbOptions = ConfigurationOptions.Parse(redisConnectionString);
        var db = Enumerable.Range(0, 10).Select(_ => ConnectionMultiplexer.Connect(dbOptions)).ToArray();
        sc.AddSingleton<ICacheRedisMultiplexer>(_ => new WrappedRedisMultiplexer(cache));
        sc.AddSingleton<IPersistentRedisMultiplexer>(_ => new WrappedRedisMultiplexer(db));

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