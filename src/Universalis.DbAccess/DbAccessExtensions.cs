using System;
using System.Threading;
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
            ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
            var maxThreads = workerThreads + completionPortThreads;

            sc.AddSingleton<IMongoClient>(new MongoClient($"mongodb://localhost:27017?socketTimeoutMS=240000&minPoolSize={Math.Min(10, maxThreads)}&maxPoolSize={maxThreads}"));

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