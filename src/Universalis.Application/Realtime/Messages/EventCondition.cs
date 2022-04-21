using System;
using System.Linq;

namespace Universalis.Application.Realtime.Messages;

public class EventCondition : IEquatable<EventCondition>
{
    private readonly string[] _channels;

    private EventCondition(string[] channels)
    {
        _channels = channels;
    }

    public bool ShouldSend(SocketMessage message)
    {
        if (_channels.Length > message.ChannelsInternal.Length)
        {
            return false;
        }

        foreach (var (cond, query) in _channels.Zip(message.ChannelsInternal))
        {
            if (!string.Equals(cond, query, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(EventCondition other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) || Equals(_channels, other._channels);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((EventCondition)obj);
    }

    public override int GetHashCode()
    {
        return (_channels != null ? _channels.GetHashCode() : 0);
    }

    public static EventCondition Parse(string condition)
    {
        var channels = condition.Split('/');
        return new EventCondition(channels);
    }
}