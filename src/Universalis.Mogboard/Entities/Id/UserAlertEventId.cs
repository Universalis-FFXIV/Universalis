namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserAlertEventId : IEquatable<UserAlertEventId>
{
    private readonly Guid _id;

    public UserAlertEventId()
    {
        _id = Guid.NewGuid();
    }

    public UserAlertEventId(Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public static UserAlertEventId Parse(string id)
    {
        var guid = Guid.Parse(id);
        return new UserAlertEventId(guid);
    }

    public static explicit operator UserAlertEventId(Guid id) => new(id);

    public static explicit operator Guid(UserAlertEventId id) => id._id;

    public bool Equals(UserAlertEventId other)
    {
        return _id.Equals(other._id);
    }

    public override bool Equals(object? obj)
    {
        return obj is UserAlertEventId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(UserAlertEventId left, UserAlertEventId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserAlertEventId left, UserAlertEventId right)
    {
        return !left.Equals(right);
    }
}