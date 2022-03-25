using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard;

public interface IMogboardSessionTable : IMogboardTable<UserSession, UserSessionId>
{
    Task<UserSession?> Get(string session, CancellationToken cancellationToken = default);
}