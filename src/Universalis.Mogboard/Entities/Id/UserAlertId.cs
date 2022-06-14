﻿namespace Universalis.Mogboard.Entities.Id;

public readonly struct UserAlertId : IEquatable<UserAlertId>
{
    private readonly Guid _id;

    public UserAlertId()
    {
        _id = Guid.NewGuid();
    }

    public UserAlertId(Guid id)
    {
        _id = id;
    }

    public override string ToString()
    {
        return _id.ToString();
    }

    public static UserAlertId Parse(string id)
    {
        var guid = Guid.Parse(id);
        return new UserAlertId(guid);
    }

    public static explicit operator UserAlertId(Guid id) => new(id);

    public static explicit operator Guid(UserAlertId id) => id._id;

    public bool Equals(UserAlertId other)
    {
        return _id.Equals(other._id);
    }

    public override bool Equals(object? obj)
    {
        return obj is UserAlertId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(UserAlertId left, UserAlertId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UserAlertId left, UserAlertId right)
    {
        return !left.Equals(right);
    }
}