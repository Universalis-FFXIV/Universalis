using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IWorldUploadCountStore
{
    Task Increment(string key, string worldName, CancellationToken cancellationToken = default);

    Task<IList<KeyValuePair<string, long>>> GetWorldUploadCounts(string key, CancellationToken cancellationToken = default);
}