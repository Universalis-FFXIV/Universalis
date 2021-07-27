using MongoDB.Driver;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess.Queries
{
    public class FlaggedUploaderQuery : DbAccessQuery<FlaggedUploader>
    {
        public string UploaderId { get; set; }

        internal override FilterDefinition<FlaggedUploader> ToFilterDefinition()
        {
            var filterBuilder = Builders<FlaggedUploader>.Filter;
            var filter = filterBuilder.Eq(o => o.UploaderId, UploaderId);
            return filter;
        }
    }
}
