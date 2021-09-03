using System.Collections;
using MongoDB.Bson.Serialization.Conventions;
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

        protected DbAccessService(
            IMongoClient client,
            string databaseName,
            string collectionName)
        {
            var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);
            var database = client.GetDatabase(databaseName);
            Collection = database.GetCollection<TDocument>(collectionName);
        }

        public Task Create(TDocument document)
        {
            return Collection.InsertOneAsync(document);
        }

        public async Task<TDocument> Retrieve(TDocumentQuery query)
        {
            return await Collection.Find(query.ToFilterDefinition()).FirstOrDefaultAsync();
        }

        public async Task Update(TDocument document, TDocumentQuery query)
        {
            // Create if non-existent
            var existing = await Retrieve(query);
            if (existing == null)
            {
                await Create(document);
                return;
            }

            // Combine sets for each updated field
            var updateBuilder = Builders<TDocument>.Update;
            var properties = typeof(TDocument).GetProperties();

            UpdateDefinition<TDocument> update = null;
            foreach (var property in properties)
            {
                var existingValue = property.GetValue(existing);
                var documentValue = property.GetValue(document);
                if (property.PropertyType.IsAssignableFrom(typeof(IEnumerable)) || documentValue != existingValue)
                {
                    var nextUpdate = updateBuilder.Set(property.Name, documentValue);
                    update = update == null
                        ? nextUpdate
                        : updateBuilder.Combine(update, nextUpdate);
                }
            }

            // Update if there are any changes
            if (update != null)
            {
                await Collection.UpdateOneAsync(query.ToFilterDefinition(), update);
            }
        }

        public Task Delete(TDocumentQuery query)
        {
            return Collection.DeleteManyAsync(query.ToFilterDefinition());
        }
    }
}