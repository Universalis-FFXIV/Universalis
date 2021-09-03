using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public interface ITrustedSourceDbAccess
    {
        public Task Create(TrustedSource document, CancellationToken cancellationToken = default);

        public Task<TrustedSource> Retrieve(TrustedSourceQuery query, CancellationToken cancellationToken = default);

        public Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts(CancellationToken cancellationToken = default);

        public Task Update(TrustedSource document, TrustedSourceQuery query, CancellationToken cancellationToken = default);

        public Task Increment(TrustedSourceQuery query, CancellationToken cancellationToken = default);

        public Task Delete(TrustedSourceQuery query, CancellationToken cancellationToken = default);
    }
}