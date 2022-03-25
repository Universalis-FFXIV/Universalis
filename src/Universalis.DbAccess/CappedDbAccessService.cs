using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Universalis.DbAccess.Queries;

namespace Universalis.DbAccess;

public class CappedDbAccessService<TDocument, TDocumentQuery>
    where TDocument : class
    where TDocumentQuery : DbAccessQuery<TDocument>
{
    protected IMongoCollection<TDocument> Collection { get; }

    protected CappedDbAccessService(
        IMongoClient client,
        string databaseName,
        string collectionName)
    {
        var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);
        var database = client.GetDatabase(databaseName);
        Collection = database.GetCollection<TDocument>(collectionName);
    }

    protected CappedDbAccessService(
        IMongoClient client,
        string databaseName,
        string collectionName,
        CreateCollectionOptions options)
    {
        var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);
        var database = client.GetDatabase(databaseName);

        if (database.ListCollectionNames().ToEnumerable().All(c => c != collectionName))
        {
            database.CreateCollection(collectionName, options);
        }

        Collection = database.GetCollection<TDocument>(collectionName);
    }

    public virtual Task Create(TDocument document, CancellationToken cancellationToken = default)
    {
        return Collection.InsertOneAsync(document, null, cancellationToken);
    }

    public virtual async Task<TDocument> Retrieve(TDocumentQuery query, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(query.ToFilterDefinition()).FirstOrDefaultAsync(cancellationToken);
    }
}