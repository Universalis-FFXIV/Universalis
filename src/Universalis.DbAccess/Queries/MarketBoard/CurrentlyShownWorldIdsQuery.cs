using MongoDB.Driver;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Queries.MarketBoard
{
    public class CurrentlyShownWorldIdsQuery : DbAccessQuery<CurrentlyShown>
    {
        public uint[] WorldIds { get; set; }

        internal override FilterDefinition<CurrentlyShown> ToFilterDefinition()
        {
            var filterBuilder = Builders<CurrentlyShown>.Filter;
            var filter = filterBuilder.In(o => o.WorldId, WorldIds);
            return filter;
        }
    }
}