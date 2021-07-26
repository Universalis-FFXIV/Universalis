using MongoDB.Driver;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Queries
{
    public class TaxRatesQuery : DbAccessQuery<TaxRates>
    {
        private const string SetName = "taxRates";

        public uint WorldId { get; set; }

        internal override FilterDefinition<TaxRates> ToFilterDefinition()
        {
            var filterBuilder = Builders<TaxRates>.Filter;
            var filter = filterBuilder.Eq(o => o.SetName, SetName) & filterBuilder.Eq(o => o.WorldId, WorldId);
            return filter;
        }
    }
}