using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IWorldUploadCountDbAccess
{
    public ValueTask<IEnumerable<WorldUploadCount>> GetWorldUploadCounts(CancellationToken cancellationToken = default);
        
    public Task Increment(WorldUploadCountQuery query, CancellationToken cancellationToken = default);
}