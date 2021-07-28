using MongoDB.Driver;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Queries.MarketBoard
{
    public class HistoryManyQuery : DbAccessQuery<History>
    {
        public uint[] WorldIds { get; init; }

        public uint ItemId { get; init; }

        internal override FilterDefinition<History> ToFilterDefinition()
        {
            var filterBuilder = Builders<History>.Filter;
            var filter = filterBuilder.In(o => o.WorldId, WorldIds) & filterBuilder.Eq(o => o.ItemId, ItemId);
            return filter;
        }
    }
}