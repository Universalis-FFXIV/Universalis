using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IFlaggedUploaderStore
{
    Task Insert(FlaggedUploader uploader, CancellationToken cancellationToken = default);

    Task<FlaggedUploader> Retrieve(string uploaderIdSha256, CancellationToken cancellationToken = default);
}