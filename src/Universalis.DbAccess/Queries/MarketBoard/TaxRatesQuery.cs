using MongoDB.Driver;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class TaxRatesQuery : DbAccessQuery<TaxRates>
{
    public uint WorldId { get; init; }

    internal override FilterDefinition<TaxRates> ToFilterDefinition()
    {
        var filterBuilder = Builders<TaxRates>.Filter;
        var filter = filterBuilder.Eq(o => o.SetName, TaxRates.DefaultSetName) & filterBuilder.Eq(o => o.WorldId, WorldId);
        return filter;
    }
}