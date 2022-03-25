using MongoDB.Driver;

namespace Universalis.DbAccess.Queries;

public abstract class DbAccessQuery<TDocument> where TDocument : class
{
    internal abstract FilterDefinition<TDocument> ToFilterDefinition();
}