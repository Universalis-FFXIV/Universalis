using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.DbAccess
{
    public static class DbAccessExtensions
    {
        public static void AddDbAccessServices(this IServiceCollection sc)
        {
            var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);

            sc.AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"));

            //sc.AddSingleton<IMostRecentlyUpdatedDbAccess, MostRecentlyUpdatedDbAccess>();
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
}