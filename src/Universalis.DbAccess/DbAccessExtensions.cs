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
            sc.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));
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