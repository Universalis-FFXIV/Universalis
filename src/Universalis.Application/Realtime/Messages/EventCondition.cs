using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Realtime.Messages;

public class EventCondition : IEquatable<EventCondition>
{
    // The event channels
    private readonly string[] _channels;
    
    // Filters on the event channel's messages
    private readonly IDictionary<string, string> _filters;

    private EventCondition(string[] channels, IDictionary<string, string> filters)
    {
        _channels = channels;
        _filters = filters;
    }

    public bool ShouldSend(SocketMessage message)
    {
        // Check that the filter channels are a subset of the message channels
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

        // Check the filters and filter values
        if (_filters.Count <= 0)
        {
            return true;
        }
        
        var properties = message.GetType()
            .GetProperties()
            .Where(prop => prop.GetGetMethod() != null)
            .Where(prop => prop.GetCustomAttribute<BsonIgnoreAttribute>() == null)
            .ToDictionary(
                prop => prop.GetCustomAttribute<BsonElementAttribute>()?.ElementName ?? prop.Name,
                prop => prop.GetGetMethod()?.Invoke(message, Array.Empty<object>())?.ToString());
        foreach (var (key, val) in _filters)
        {
            if (!properties.TryGetValue(key, out var test) || test != val)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether or not this event condition can be replaced with the provided event condition.
    /// This event condition can be replaced with the provided event condition if the other condition
    /// is less-specific than this one.
    /// </summary>
    /// <param name="other">The event condition to compare this to.</param>
    public bool IsReplaceableWith(EventCondition other)
    {
        if (_channels.Length < other._channels.Length || _filters.Count < other._filters.Count)
        {
            return false;
        }
        
        for (var i = 0; i < other._channels.Length; i++)
        {
            if (!string.Equals(other._channels[i], _channels[i], StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }
        }

        foreach (var (k, v) in other._filters)
        {
            if (!_filters.ContainsKey(k) || !string.Equals(_filters[k], v, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }
        }

        return true;
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
        return _channels != null ? _channels.GetHashCode() : 0;
    }
    
    public static EventCondition Parse(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            throw new ArgumentException("Input may not be empty.", nameof(condition));
        }
        
        // Should be in a format like "listings/add{world=74, item=5}"
        var channels = new string(condition.TakeWhile(c => c != '{').ToArray()).Trim().Split('/');

        var filters = new Dictionary<string, string>();
        var filtersStart = condition.IndexOf('{');
        var filtersEnd = condition.IndexOf('}');
        if (filtersStart != -1 && filtersEnd != -1)
        {
            var filtersStr = condition[(filtersStart + 1)..filtersEnd];
            filters = filtersStr
                .Split(',')
                .Select(s => s.Trim().Split('=').Select(ss => ss.Trim()).ToArray())
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);
        }

        return new EventCondition(channels, filters);
    }
}