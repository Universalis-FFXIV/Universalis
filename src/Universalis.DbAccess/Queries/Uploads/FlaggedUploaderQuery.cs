using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.Uploads;

public class FlaggedUploaderQuery : DbAccessQuery<FlaggedUploader>
{
    public string UploaderIdHash { get; set; }

    internal override FilterDefinition<FlaggedUploader> ToFilterDefinition()
    {
        var filterBuilder = Builders<FlaggedUploader>.Filter;
        var filter = filterBuilder.Eq(o => o.UploaderIdHash, UploaderIdHash);
        return filter;
    }
}