using MongoDB.Driver;
using System.Threading.Tasks;
using Universalis.DbAccess.Query;

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

        public Task<TDocument> Retrieve(TDocumentQuery query)
        {
            return Collection.Find(query.ToFilterDefinition()).FirstAsync();
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