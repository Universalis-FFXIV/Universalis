using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Identity;

public class MogboardUser
{
    private readonly User _user;

    internal MogboardUser(User user)
    {
        _user = user;
    }

    public UserId GetId()
    {
        return _user.Id;
    }

    public string GetUsername()
    {
        return _user.Username!;
    }

    public string GetEmail()
    {
        return _user.Email!;
    }

    public string? GetAvatar()
    {
        return _user.Avatar;
    }

    public DiscordSso? GetDiscordSso()
    {
        if (_user.Sso != "discord") return null;
        return new DiscordSso
        {
            Id = _user.SsoDiscordId,
            Avatar = _user.SsoDiscordAvatar,
        };
    }

    public DateTimeOffset GetCreationTime()
    {
        return _user.Added;
    }

    public DateTimeOffset GetLastOnlineTime()
    {
        return _user.LastOnline;
    }

    public bool IsAdmin()
    {
        return _user.Admin;
    }
}