using MongoDB.Driver;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries;

namespace Universalis.DbAccess
{
    public abstract class DbAccessService<TDocument, TDocumentQuery> : CappedDbAccessService<TDocument, TDocumentQuery>
        where TDocument : class
        where TDocumentQuery : DbAccessQuery<TDocument>
    {
        protected DbAccessService(IMongoClient client, string databaseName, string collectionName) : base(client, databaseName, collectionName) { }

        protected DbAccessService(IMongoClient client, string databaseName, string collectionName, CreateCollectionOptions options) : base(client, databaseName, collectionName, options) { }

        public virtual async Task Update(TDocument document, TDocumentQuery query, CancellationToken cancellationToken = default)
        {
            // Create if non-existent
            var existing = await Retrieve(query, cancellationToken);
            if (existing == null)
            {
                await Create(document, cancellationToken);
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
                await Collection.UpdateOneAsync(query.ToFilterDefinition(), update, cancellationToken: cancellationToken);
            }
        }

        public virtual Task Delete(TDocumentQuery query, CancellationToken cancellationToken = default)
        {
            return Collection.DeleteManyAsync(query.ToFilterDefinition(), cancellationToken);
        }
    }
}