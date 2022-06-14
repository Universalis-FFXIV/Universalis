using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IFlaggedUploaderDbAccess
{
    public Task Create(FlaggedUploader document, CancellationToken cancellationToken = default);

    public Task<FlaggedUploader> Retrieve(FlaggedUploaderQuery query, CancellationToken cancellationToken = default);
}