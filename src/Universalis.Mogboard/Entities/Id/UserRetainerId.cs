namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserRetainerId
{
    private readonly System.Guid _id;

    public UserRetainerId(System.Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is UserRetainerId other && _id.Equals(other._id);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static UserRetainerId Parse(string id)
    {
        var guid = System.Guid.Parse(id);
        return new UserRetainerId(guid);
    }

    public static explicit operator UserRetainerId(System.Guid id) => new(id);

    public static explicit operator System.Guid(UserRetainerId id) => id._id;

    public static bool operator ==(UserRetainerId left, UserRetainerId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserRetainerId left, UserRetainerId right)
    {
        return !(left == right);
    }
}