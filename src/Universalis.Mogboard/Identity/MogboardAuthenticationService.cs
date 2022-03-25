using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Identity;

public class MogboardAuthenticationService : IMogboardAuthenticationService
{
    private readonly IMogboardTable<User, UserId> _users;
    private readonly IMogboardSessionTable _sessions;

    public MogboardAuthenticationService(IMogboardTable<User, UserId> users, IMogboardSessionTable sessions)
    {
        _users = users;
        _sessions = sessions;
    }

    public async Task<MogboardUser> Authenticate(string session, CancellationToken cancellationToken = default)
    {
        // TODO: Perform a single query for this
        var sessionInst = await _sessions.Get(session, cancellationToken);
        if (sessionInst == null)
        {
            throw new InvalidOperationException("No session found.");
        }

        if (sessionInst.UserId == null)
        {
            throw new InvalidOperationException("Session instance does not contain a user ID.");
        }

        var userInst = await _users.Get(sessionInst.UserId.Value, cancellationToken);
        if (userInst == null)
        {
            throw new InvalidOperationException("No user found for the retrieved session.");
        }

        return new MogboardUser(userInst);
    }
}