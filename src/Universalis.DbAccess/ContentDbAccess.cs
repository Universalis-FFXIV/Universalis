using Universalis.DbAccess.Queries;
using Universalis.Entities;

namespace Universalis.DbAccess
{
    public class ContentDbAccess : DbAccessService<Content, ContentQuery>, IContentDbAccess
    {
        public ContentDbAccess() : base("universalis", "content") { }

        public ContentDbAccess(string databaseName) : base(databaseName, "content") { }
    }
}