using MongoDB.Driver;
using Universalis.DbAccess.Queries;
using Universalis.Entities;

namespace Universalis.DbAccess
{
    public class ContentDbAccess : DbAccessService<Content, ContentQuery>, IContentDbAccess
    {
        public ContentDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler) : base(client, throttler, Constants.DatabaseName, "content") { }

        public ContentDbAccess(IMongoClient client, IConnectionThrottlingPipeline throttler, string databaseName) : base(client, throttler, databaseName, "content") { }
    }
}