using System.Threading.Tasks;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads;

public interface IUploadLogStore
{
    Task LogAction(UploadLogEntry entry);
}