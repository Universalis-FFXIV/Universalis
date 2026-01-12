using System;
using System.Collections.Generic;
using Microsoft.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Realtime.Messages;

/// <summary>
/// Interface for messages that can be filtered by subscription conditions.
/// Implementing this interface allows filter matching without reflection.
/// </summary>
public interface IFilterableMessage
{
    /// <summary>
    /// Returns filter-relevant properties as key-value pairs.
    /// Keys should match BsonElement names (e.g., "item", "world").
    /// </summary>
    IReadOnlyDictionary<string, string> GetFilterValues();
}

public abstract class SocketMessage : IFilterableMessage
{
    [BsonElement("event")]
    public string Event => string.Join('/', ChannelsInternal);

    [BsonIgnore]
    public string[] ChannelsInternal { get; }

    [BsonIgnore]
    internal byte[]? CachedSerializedBytes { get; set; }

    protected SocketMessage(params string[] channels)
    {
        ChannelsInternal = channels;
    }

    /// <summary>
    /// Returns pre-cached serialized bytes if available, otherwise serializes on-the-fly.
    /// </summary>
    internal ReadOnlyMemory<byte> GetSerializedBytes(RecyclableMemoryStreamManager pool)
    {
        if (CachedSerializedBytes != null)
            return CachedSerializedBytes;

        using var stream = pool.GetStream();
        using var writer = new BsonBinaryWriter(stream);
        BsonSerializer.Serialize(writer, GetType(), this);
        return stream.ToArray();
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyFilters =
        new Dictionary<string, string>();

    /// <summary>
    /// Returns filter-relevant properties for subscription matching.
    /// Override in derived classes to provide filterable properties.
    /// </summary>
    public virtual IReadOnlyDictionary<string, string> GetFilterValues() => EmptyFilters;
}