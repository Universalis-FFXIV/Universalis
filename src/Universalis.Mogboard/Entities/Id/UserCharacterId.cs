namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserCharacterId : IEquatable<UserCharacterId>
{
    private readonly Guid _id;

    public UserCharacterId()
    {
        _id = Guid.NewGuid();
    }

    public UserCharacterId(Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public static UserCharacterId Parse(string id)
    {
        var guid = Guid.Parse(id);
        return new UserCharacterId(guid);
    }

    public static explicit operator UserCharacterId(Guid id) => new(id);

    public static explicit operator Guid(UserCharacterId id) => id._id;

    public bool Equals(UserCharacterId other)
    {
        return _id.Equals(other._id);
    }

    public override bool Equals(object? obj)
    {
        return obj is UserCharacterId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(UserCharacterId left, UserCharacterId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserCharacterId left, UserCharacterId right)
    {
        return !left.Equals(right);
    }
}