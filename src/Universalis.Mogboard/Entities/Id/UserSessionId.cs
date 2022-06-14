﻿namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserSessionId : IEquatable<UserSessionId>
{
    private readonly Guid _id;

    public UserSessionId()
    {
        _id = Guid.NewGuid();
    }

    public UserSessionId(Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public static UserSessionId Parse(string id)
    {
        var guid = Guid.Parse(id);
        return new UserSessionId(guid);
    }

    public static explicit operator UserSessionId(Guid id) => new(id);

    public static explicit operator Guid(UserSessionId id) => id._id;

    public bool Equals(UserSessionId other)
    {
        return _id.Equals(other._id);
    }

    public override bool Equals(object? obj)
    {
        return obj is UserSessionId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(UserSessionId left, UserSessionId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserSessionId left, UserSessionId right)
    {
        return !left.Equals(right);
    }
}