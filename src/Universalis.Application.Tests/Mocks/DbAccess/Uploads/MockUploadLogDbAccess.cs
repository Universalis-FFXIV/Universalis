using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads;

public class MockUploadLogDbAccess : IUploadLogDbAccess
{
    private readonly ConcurrentQueue<UploadLogEntry> _loggedActions = new();

    public IReadOnlyCollection<UploadLogEntry> LoggedActions => _loggedActions.ToList();

    public Task LogAction(UploadLogEntry entry)
    {
        _loggedActions.Enqueue(entry);
        return Task.CompletedTask;
    }
}