using MongoDB.Driver;
using Universalis.Entities;

namespace Universalis.DbAccess.Queries;

public class ContentQuery : DbAccessQuery<Content>
{
    public string ContentId { get; set; }

    internal override FilterDefinition<Content> ToFilterDefinition()
    {
        var filterBuilder = Builders<Content>.Filter;
        var filter = filterBuilder.Eq(o => o.ContentId, ContentId);
        return filter;
    }
}