using System.Threading.Tasks;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockUploadLogDbAccess : IUploadLogDbAccess
{
    public Task LogAction(UploadLogEntry entry)
    {
        return Task.CompletedTask;
    }
}