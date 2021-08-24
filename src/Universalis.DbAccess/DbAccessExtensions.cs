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
            sc.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017?maxpoolsize=10000"));

            sc.AddScoped<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();
            sc.AddScoped<IHistoryDbAccess, HistoryDbAccess>();
            sc.AddScoped<IContentDbAccess, ContentDbAccess>();
            sc.AddScoped<ITaxRatesDbAccess, TaxRatesDbAccess>();
            sc.AddScoped<ITrustedSourceDbAccess, TrustedSourceDbAccess>();
            sc.AddScoped<IFlaggedUploaderDbAccess, FlaggedUploaderDbAccess>();
            sc.AddScoped<IWorldUploadCountDbAccess, WorldUploadCountDbAccess>();
            sc.AddScoped<IRecentlyUpdatedItemsDbAccess, RecentlyUpdatedItemsDbAccess>();
            sc.AddScoped<IUploadCountHistoryDbAccess, UploadCountHistoryDbAccess>();
        }
    }
}