using MongoDB.Driver;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard
{
    public class TaxRatesDbAccess : DbAccessService<TaxRates, TaxRatesQuery>, ITaxRatesDbAccess
    {
        public TaxRatesDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "extraData") { }

        public TaxRatesDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "extraData") { }
    }
}