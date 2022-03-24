namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserCharacterId
{
    private readonly System.Guid _id;

    public UserCharacterId(System.Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is UserCharacterId other && _id.Equals(other._id);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static UserCharacterId Parse(string id)
    {
        var guid = System.Guid.Parse(id);
        return new UserCharacterId(guid);
    }

    public static explicit operator UserCharacterId(System.Guid id) => new(id);

    public static explicit operator System.Guid(UserCharacterId id) => id._id;

    public static bool operator ==(UserCharacterId left, UserCharacterId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserCharacterId left, UserCharacterId right)
    {
        return !(left == right);
    }
}