using MongoDB.Driver;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Queries
{
    public class CurrentlyShownQuery : DbAccessQuery<CurrentlyShown>
    {
        public uint WorldId { get; init; }

        public uint ItemId { get; init; }

        internal override FilterDefinition<CurrentlyShown> ToFilterDefinition()
        {
            var filterBuilder = Builders<CurrentlyShown>.Filter;
            var filter = filterBuilder.Eq(o => o.WorldId, WorldId) & filterBuilder.Eq(o => o.ItemId, ItemId);
            return filter;
        }
    }
}