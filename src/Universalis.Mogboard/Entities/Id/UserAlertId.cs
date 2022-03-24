namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserAlertId
{
    private readonly System.Guid _id;

    public UserAlertId(System.Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is UserAlertId other && _id.Equals(other._id);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static UserAlertId Parse(string id)
    {
        var guid = System.Guid.Parse(id);
        return new UserAlertId(guid);
    }

    public static explicit operator UserAlertId(System.Guid id) => new(id);

    public static explicit operator System.Guid(UserAlertId id) => id._id;

    public static bool operator ==(UserAlertId left, UserAlertId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserAlertId left, UserAlertId right)
    {
        return !(left == right);
    }
}