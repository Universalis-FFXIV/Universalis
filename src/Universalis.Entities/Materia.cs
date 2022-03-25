using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Universalis.Entities;

public class Materia : IEquatable<Materia>
{
    [BsonElement("slotID")]
    public uint SlotId { get; init; }

    [BsonElement("materiaID")]
    public uint MateriaId { get; init; }

    public bool Equals(Materia other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SlotId == other.SlotId && MateriaId == other.MateriaId;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Materia)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SlotId, MateriaId);
    }
}