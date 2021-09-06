using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface IMostRecentlyUpdatedDbAccess
    {
        public Task Create(WorldItemUpload document, CancellationToken cancellationToken = default);

        public Task<WorldItemUpload> Retrieve(MostRecentlyUpdatedQuery query, CancellationToken cancellationToken = default);

        public Task<IList<WorldItemUpload>> RetrieveMany(int? count = null, CancellationToken cancellationToken = default);
    }
}