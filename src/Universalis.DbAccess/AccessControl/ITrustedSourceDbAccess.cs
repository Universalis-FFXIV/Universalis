using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.AccessControl;

namespace Universalis.DbAccess.AccessControl;

public interface ITrustedSourceDbAccess
{
    public Task Create(ApiKey document, CancellationToken cancellationToken = default);

    public Task<ApiKey> Retrieve(TrustedSourceQuery query, CancellationToken cancellationToken = default);

    public Task Increment(TrustedSourceQuery query, CancellationToken cancellationToken = default);

    public Task<IEnumerable<TrustedSourceNoApiKey>> GetUploaderCounts(CancellationToken cancellationToken = default);
}