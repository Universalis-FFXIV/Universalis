using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess
{
    public class TaxRatesDbAccess : DbAccessService<TaxRates, TaxRatesQuery>
    {
        public TaxRatesDbAccess() : base("universalis", "extraData") { }
    }
}