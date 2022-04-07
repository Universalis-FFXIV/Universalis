using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.DbAccess;

public static class DbAccessExtensions
{
    public static void AddDbAccessServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddSingleton<IMongoClient>(_ => new MongoClient(configuration["MongoDbConnectionString"]));

        sc.AddSingleton<IMostRecentlyUpdatedDbAccess, MostRecentlyUpdatedDbAccess>();
        sc.AddSingleton<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();
        sc.AddSingleton<IHistoryDbAccess, HistoryDbAccess>();
        sc.AddSingleton<IContentDbAccess, ContentDbAccess>();
        sc.AddSingleton<ITaxRatesDbAccess, TaxRatesDbAccess>();
        sc.AddSingleton<ITrustedSourceDbAccess, TrustedSourceDbAccess>();
        sc.AddSingleton<IFlaggedUploaderDbAccess, FlaggedUploaderDbAccess>();
        sc.AddSingleton<IWorldUploadCountDbAccess, WorldUploadCountDbAccess>();
        sc.AddSingleton<IRecentlyUpdatedItemsDbAccess, RecentlyUpdatedItemsDbAccess>();
        sc.AddSingleton<IUploadCountHistoryDbAccess, UploadCountHistoryDbAccess>();
    }
}