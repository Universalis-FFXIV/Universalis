using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class FlaggedUploaderDbAccess : IFlaggedUploaderDbAccess
{
    private readonly IFlaggedUploaderStore _flaggedUploaders;

    public FlaggedUploaderDbAccess(IFlaggedUploaderStore flaggedUploaders)
    {
        _flaggedUploaders = flaggedUploaders;
    }
    
    public Task Create(FlaggedUploader document, CancellationToken cancellationToken = default)
    {
        return _flaggedUploaders.Insert(document, cancellationToken);
    }

    public Task<FlaggedUploader> Retrieve(FlaggedUploaderQuery query, CancellationToken cancellationToken = default)
    {
        return _flaggedUploaders.Retrieve(query.UploaderIdSha256, cancellationToken);
    }
}