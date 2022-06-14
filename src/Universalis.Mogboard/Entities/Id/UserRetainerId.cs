namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserRetainerId : IEquatable<UserRetainerId>
{
    private readonly Guid _id;

    public UserRetainerId()
    {
        _id = Guid.NewGuid();
    }

    public UserRetainerId(Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public static UserRetainerId Parse(string id)
    {
        var guid = Guid.Parse(id);
        return new UserRetainerId(guid);
    }

    public static explicit operator UserRetainerId(Guid id) => new(id);

    public static explicit operator Guid(UserRetainerId id) => id._id;

    public bool Equals(UserRetainerId other)
    {
        return _id.Equals(other._id);
    }

    public override bool Equals(object? obj)
    {
        return obj is UserRetainerId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(UserRetainerId left, UserRetainerId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserRetainerId left, UserRetainerId right)
    {
        return !left.Equals(right);
    }
}