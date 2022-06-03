using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StackExchange.Redis;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.DbAccess;

public static class DbAccessExtensions
{
    public static void AddDbAccessServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddSingleton<IMongoClient>(new MongoClient(configuration["MongoDbConnectionString"]));
        sc.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration["RedisConnectionString"]));

        sc.AddSingleton<IMostRecentlyUpdatedDbAccess, MostRecentlyUpdatedDbAccess>();
        sc.AddSingleton<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();
        sc.AddSingleton<IHistoryDbAccess, HistoryDbAccess>();
        sc.AddSingleton<IContentDbAccess, ContentDbAccess>();
        sc.AddSingleton<ITaxRatesDbAccess, TaxRatesDbAccess>();
        sc.AddSingleton<IFlaggedUploaderDbAccess, FlaggedUploaderDbAccess>();
        sc.AddSingleton<IWorldUploadCountDbAccess, WorldUploadCountDbAccess>();
        sc.AddSingleton<IUploadCountHistoryDbAccess, UploadCountHistoryDbAccess>();

        sc.AddSingleton<ISourceUploadCountStore, TrustedSourceUploadCountStore>();
        sc.AddSingleton<ITrustedSourceDbAccess, TrustedSourceDbAccess>();
        
        sc.AddSingleton<IRecentlyUpdatedItemsStore, RecentlyUpdatedItemsStore>();
        sc.AddSingleton<IRecentlyUpdatedItemsDbAccess, RecentlyUpdatedItemsDbAccess>();
    }
}