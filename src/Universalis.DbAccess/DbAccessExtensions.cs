using Microsoft.Extensions.DependencyInjection;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.DbAccess
{
    public static class DbAccessExtensions
    {
        public static void AddDbAccessServices(this IServiceCollection sc)
        {
            sc.AddTransient<ICurrentlyShownDbAccess, CurrentlyShownDbAccess>();
            sc.AddTransient<IHistoryDbAccess, HistoryDbAccess>();
            sc.AddTransient<IContentDbAccess, ContentDbAccess>();
            sc.AddTransient<ITaxRatesDbAccess, TaxRatesDbAccess>();
            sc.AddTransient<ITrustedSourceDbAccess, TrustedSourceDbAccess>();
            sc.AddTransient<IFlaggedUploaderDbAccess, FlaggedUploaderDbAccess>();
            sc.AddTransient<IWorldUploadCountDbAccess, WorldUploadCountDbAccess>();
            sc.AddTransient<IRecentlyUpdatedItemsDbAccess, RecentlyUpdatedItemsDbAccess>();
        }
    }
}