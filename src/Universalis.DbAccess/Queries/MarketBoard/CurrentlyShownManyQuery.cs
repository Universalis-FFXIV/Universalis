using MongoDB.Driver;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class CurrentlyShownManyQuery : DbAccessQuery<CurrentlyShown>
{
    public uint[] WorldIds { get; init; }

    public uint ItemId { get; init; }

    internal override FilterDefinition<CurrentlyShown> ToFilterDefinition()
    {
        var filterBuilder = Builders<CurrentlyShown>.Filter;
        var filter = filterBuilder.In(o => o.WorldId, WorldIds) & filterBuilder.Eq(o => o.ItemId, ItemId);
        return filter;
    }
}