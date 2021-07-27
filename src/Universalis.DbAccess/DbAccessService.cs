using MongoDB.Driver;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries;

namespace Universalis.DbAccess
{
    public abstract class DbAccessService<TDocument, TDocumentQuery>
        where TDocument : class
        where TDocumentQuery : DbAccessQuery<TDocument>
    {
        protected IMongoCollection<TDocument> Collection { get; }

        protected DbAccessService(string databaseName, string collectionName)
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase(databaseName);
            Collection = database.GetCollection<TDocument>(collectionName);
        }

        public Task Create(TDocument document)
        {
            return Collection.InsertOneAsync(document);
        }

        public async Task<TDocument> Retrieve(TDocumentQuery query)
        {
            var cursor = await Collection.FindAsync(query.ToFilterDefinition());
            return await cursor.FirstAsync();
        }

        public async Task Update(TDocument document, TDocumentQuery query)
        {
            await Delete(query);
            await Create(document);
        }

        public Task Delete(TDocumentQuery query)
        {
            return Collection.DeleteManyAsync(query.ToFilterDefinition());
        }
    }
}