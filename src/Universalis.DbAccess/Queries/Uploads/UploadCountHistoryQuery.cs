using MongoDB.Driver;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Queries.Uploads;

public class UploadCountHistoryQuery : DbAccessQuery<UploadCountHistory>
{
    internal override FilterDefinition<UploadCountHistory> ToFilterDefinition()
    {
        var filterBuilder = Builders<UploadCountHistory>.Filter;
        var filter = filterBuilder.Eq(o => o.SetName, UploadCountHistory.DefaultSetName);
        return filter;
    }
}