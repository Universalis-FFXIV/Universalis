using MongoDB.Driver;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class FlaggedUploaderDbAccess : DbAccessService<FlaggedUploader, FlaggedUploaderQuery>, IFlaggedUploaderDbAccess
    {
        public FlaggedUploaderDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler) : base(client, throttler, Constants.DatabaseName, "blacklist") { }

        public FlaggedUploaderDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler, string databaseName) : base(client, throttler, databaseName, "blacklist") { }
    }
}