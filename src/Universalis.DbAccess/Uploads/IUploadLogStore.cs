using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IUploadLogStore
{
    Task LogAction(UploadLogEntry entry);

    Task LogActions(IReadOnlyCollection<UploadLogEntry> entries, CancellationToken cancellationToken = default);
}