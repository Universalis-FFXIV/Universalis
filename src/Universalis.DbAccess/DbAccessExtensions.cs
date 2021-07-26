using Microsoft.Extensions.DependencyInjection;

namespace Universalis.DbAccess
{
    public static class DbAccessExtensions
    {
        public static void AddDbAccessServices(this IServiceCollection sc)
        {
            sc.AddSingleton<ITaxRatesDbAccess, TaxRatesDbAccess>();
        }
    }
}