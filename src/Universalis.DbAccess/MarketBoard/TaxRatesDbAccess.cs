using MongoDB.Driver;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public class TaxRatesDbAccess : DbAccessService<TaxRates, TaxRatesQuery>, ITaxRatesDbAccess
    {
        public TaxRatesDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler) : base(client, throttler, Constants.DatabaseName, "extraData") { }

        public TaxRatesDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler, string databaseName) : base(client, throttler, databaseName, "extraData") { }
    }
}