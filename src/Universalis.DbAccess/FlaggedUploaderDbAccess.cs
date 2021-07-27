using Universalis.DbAccess.Queries;
using Universalis.Entities.Uploaders;

namespace Universalis.DbAccess
{
    public class FlaggedUploaderDbAccess : DbAccessService<FlaggedUploader, FlaggedUploaderQuery>, IFlaggedUploaderDbAccess
    {
        public FlaggedUploaderDbAccess() : base("universalis", "blacklist") { }
    }
}