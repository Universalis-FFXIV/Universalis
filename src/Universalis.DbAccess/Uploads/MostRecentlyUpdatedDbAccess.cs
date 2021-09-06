using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class MostRecentlyUpdatedDbAccess : CappedDbAccessService<WorldItemUpload, MostRecentlyUpdatedQuery>, IMostRecentlyUpdatedDbAccess
    {
        public static readonly int MaxSize = 15 * 1024;
        public static readonly int MaxDocuments = 200;

        public MostRecentlyUpdatedDbAccess(IMongoClient client) : base(client, Constants.DatabaseName, "mostRecentlyUpdated", new CreateCollectionOptions
        {
            Capped = true,
            MaxSize = MaxSize,
            MaxDocuments = MaxDocuments,
        }) { }

        public MostRecentlyUpdatedDbAccess(IMongoClient client, string databaseName) : base(client, databaseName, "mostRecentlyUpdated", new CreateCollectionOptions
        {
            Capped = true,
            MaxSize = MaxSize,
            MaxDocuments = MaxDocuments,
        }) { }

        public async Task<IList<WorldItemUpload>> RetrieveMany(int? count = null, CancellationToken cancellationToken = default)
        {
            return await Collection.Find(FilterDefinition<WorldItemUpload>.Empty)
                .Limit(count)
                .ToListAsync(cancellationToken);
        }
    }
}