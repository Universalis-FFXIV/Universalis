using MongoDB.Driver;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Queries.MarketBoard
{
    public class HistoryQuery : DbAccessQuery<History>
    {
        public uint WorldId { get; init; }

        public uint ItemId { get; init; }

        internal override FilterDefinition<History> ToFilterDefinition()
        {
            var filterBuilder = Builders<History>.Filter;
            var filter = filterBuilder.Eq(o => o.WorldId, WorldId) & filterBuilder.Eq(o => o.ItemId, ItemId);
            return filter;
        }
    }
}