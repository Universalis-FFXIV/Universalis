namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserRetainerId
{
    private readonly Guid _id;

    public UserRetainerId()
    {
        _id = new Guid();
    }

    public UserRetainerId(Guid id)
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
        var guid = Guid.Parse(id);
        return new UserRetainerId(guid);
    }

    public static explicit operator UserRetainerId(Guid id) => new(id);

    public static explicit operator Guid(UserRetainerId id) => id._id;

    public static bool operator ==(UserRetainerId left, UserRetainerId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserRetainerId left, UserRetainerId right)
    {
        return !(left == right);
    }
}