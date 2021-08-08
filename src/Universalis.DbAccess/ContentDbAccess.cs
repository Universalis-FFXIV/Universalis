using MongoDB.Driver;
using Universalis.DbAccess.Queries;
using Universalis.Entities;

namespace Universalis.DbAccess
{
    public class ContentDbAccess : DbAccessService<Content, ContentQuery>, IContentDbAccess
    {
        public ContentDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "content") { }

        public ContentDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "content") { }
    }
}