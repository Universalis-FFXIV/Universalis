using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class FlaggedUploaderDbAccess : DbAccessService<FlaggedUploader, FlaggedUploaderQuery>, IFlaggedUploaderDbAccess
    {
        public FlaggedUploaderDbAccess() : base("universalis", "blacklist") { }

        public FlaggedUploaderDbAccess(string databaseName) : base(databaseName, "blacklist") { }
    }
}