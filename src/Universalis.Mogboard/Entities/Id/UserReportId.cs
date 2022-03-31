namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserReportId
{
    private readonly Guid _id;

    public UserReportId()
    {
        _id = Guid.NewGuid();
    }

    public UserReportId(Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is UserReportId other && _id.Equals(other._id);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static UserReportId Parse(string id)
    {
        var guid = Guid.Parse(id);
        return new UserReportId(guid);
    }

    public static explicit operator UserReportId(Guid id) => new(id);

    public static explicit operator Guid(UserReportId id) => id._id;

    public static bool operator ==(UserReportId left, UserReportId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserReportId left, UserReportId right)
    {
        return !(left == right);
    }
}