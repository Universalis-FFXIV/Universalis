namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserId
{
    private readonly Guid _id;

    public UserId()
    {
        _id = Guid.NewGuid();
    }

    public UserId(Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is UserId other && _id.Equals(other._id);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static UserId Parse(string id)
    {
        var guid = Guid.Parse(id);
        return new UserId(guid);
    }

    public static explicit operator UserId(Guid id) => new(id);

    public static explicit operator Guid(UserId id) => id._id;

    public static bool operator ==(UserId left, UserId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserId left, UserId right)
    {
        return !(left == right);
    }
}