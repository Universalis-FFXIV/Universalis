using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Universalis.Application.Realtime.Messages;

public class EventCondition : IEquatable<EventCondition>
{
    private readonly string[] _channels;
    private readonly IDictionary<string, string> _filters;

    private EventCondition(string[] channels, IDictionary<string, string> filters)
    {
        _channels = channels;
        _filters = filters;
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

        if (_filters.Count > 0)
        {
            var properties = message.GetType()
                .GetProperties()
                .Where(prop => prop.GetGetMethod() != null)
                .ToDictionary(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name,
                    prop => prop.GetGetMethod()?.Invoke(message, Array.Empty<object>())?.ToString());
            foreach (var (key, val) in _filters)
            {
                if (properties[key] != val)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static EventCondition Parse(string condition)
    {
        // Should be in a format like "listings/add{world=74, item=5}"
        var channels = new string(condition.TakeWhile(c => c != '{').ToArray()).Trim().Split('/');

        var filters = new Dictionary<string, string>();
        var filtersStart = condition.IndexOf('{');
        var filtersEnd = condition.IndexOf('}');
        if (filtersStart != -1 && filtersEnd != -1)
        {
            var filtersStr = condition[filtersStart..filtersEnd];
            filters = filtersStr
                .Split(',')
                .Select(s => s.Trim().Split('='))
                .ToDictionary(p => p[0], p => p[1]);
        }

        return new EventCondition(channels, filters);
    }

    public bool Equals(EventCondition other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(_channels, other._channels) && Equals(_filters, other._filters);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((EventCondition)obj);
    }

    public override int GetHashCode()
    {
        return (_channels != null ? _channels.GetHashCode() : 0);
    }
}