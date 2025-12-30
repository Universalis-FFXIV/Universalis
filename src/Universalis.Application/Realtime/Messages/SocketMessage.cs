using System;
using Microsoft.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Realtime.Messages;

public abstract class SocketMessage
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
}