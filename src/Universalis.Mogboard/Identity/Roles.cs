namespace Universalis.Mogboard.Identity;

[Flags]
public enum Roles
{
    User = 1 << 0,
    Admin = 1 << 1,
}