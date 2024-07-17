using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text.Json.Serialization;
using MemoryPack;

namespace Universalis.Entities;

[MemoryPackable]
public partial class Materia : IEquatable<Materia>
{
    [BsonElement("slotID")]
    [JsonPropertyName("slot_id")]
    public int SlotId { get; init; }

    [BsonElement("materiaID")]
    [JsonPropertyName("materia_id")]
    public int MateriaId { get; init; }

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