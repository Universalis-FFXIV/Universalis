using MongoDB.Driver;

namespace Universalis.DbAccess.Query
{
    public abstract class DbAccessQuery<TDocument> where TDocument : class
    {
        internal abstract FilterDefinition<TDocument> ToFilterDefinition();
    }
}