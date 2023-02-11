using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public class UploadLogDbAccess : IUploadLogDbAccess
{
    private readonly IUploadLogStore _store;

    public UploadLogDbAccess(IUploadLogStore store)
    {
        _store = store;
    }

    public Task LogAction(UploadLogEntry entry)
    {
        return _store.LogAction(entry);
    }
}