using MongoDB.Driver;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class FlaggedUploaderDbAccess : DbAccessService<FlaggedUploader, FlaggedUploaderQuery>, IFlaggedUploaderDbAccess
    {
        public FlaggedUploaderDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "blacklist") { }

        public FlaggedUploaderDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "blacklist") { }
    }
}