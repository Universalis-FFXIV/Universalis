namespace Universalis.Mogboard.Identity;

public interface IMogboardAuthenticationService
{
    Task<MogboardUser> Authenticate(string session, CancellationToken cancellationToken = default);
}