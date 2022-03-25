using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads;

public class WorldUploadCount : ExtraData, IEquatable<WorldUploadCount>
{
    public static readonly string DefaultSetName = "worldUploadCount";

    [BsonElement("count")]
    public double Count { get; init; }

    [BsonElement("worldName")]
    public string WorldName { get; init; }

    public WorldUploadCount() : base(DefaultSetName) { }

    public bool Equals(WorldUploadCount other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Math.Abs(Count - other.Count) < 0.1 && WorldName == other.WorldName;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((WorldUploadCount) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Count, WorldName);
    }
}