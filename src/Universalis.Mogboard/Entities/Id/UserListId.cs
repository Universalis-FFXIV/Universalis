namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserListId
{
    private readonly System.Guid _id;

    public UserListId(System.Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is UserListId other && _id.Equals(other._id);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static UserListId Parse(string id)
    {
        var guid = System.Guid.Parse(id);
        return new UserListId(guid);
    }

    public static explicit operator UserListId(System.Guid id) => new(id);

    public static explicit operator System.Guid(UserListId id) => id._id;

    public static bool operator ==(UserListId left, UserListId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserListId left, UserListId right)
    {
        return !(left == right);
    }
}