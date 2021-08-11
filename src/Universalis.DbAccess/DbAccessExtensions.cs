using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.DbAccess
{
    public static class DbAccessExtensions
    {
        public static void AddDbAccessServices(this IServiceCollection sc)
        {
            sc.AddSingleton<IMongoClient>(new MongoClient(new MongoClientSettings
            {
                Server = new MongoServerAddress("mongodb://localhost:27017"),
                MaxConnectionPoolSize = 500,
            }));

            sc.AddSingleton<IConnectionThrottlingPipeline, ConnectionThrottlingPipeline>();

            sc.AddTransient<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();
            sc.AddTransient<IHistoryDbAccess, HistoryDbAccess>();
            sc.AddTransient<IContentDbAccess, ContentDbAccess>();
            sc.AddTransient<ITaxRatesDbAccess, TaxRatesDbAccess>();
            sc.AddTransient<ITrustedSourceDbAccess, TrustedSourceDbAccess>();
            sc.AddTransient<IFlaggedUploaderDbAccess, FlaggedUploaderDbAccess>();
            sc.AddTransient<IWorldUploadCountDbAccess, WorldUploadCountDbAccess>();
            sc.AddTransient<IRecentlyUpdatedItemsDbAccess, RecentlyUpdatedItemsDbAccess>();
            sc.AddTransient<IUploadCountHistoryDbAccess, UploadCountHistoryDbAccess>();
        }
    }
}