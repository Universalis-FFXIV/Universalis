using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IUploadLogDbAccess
{
    Task LogAction(UploadLogEntry entry);
}