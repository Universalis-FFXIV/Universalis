using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IUploadCountHistoryDbAccess
{
    public Task Create(UploadCountHistory document, CancellationToken cancellationToken = default);

    public Task<UploadCountHistory> Retrieve(UploadCountHistoryQuery query, CancellationToken cancellationToken = default);

    public Task Update(double lastPush, List<double> uploadCountByDay, CancellationToken cancellationToken = default);
}