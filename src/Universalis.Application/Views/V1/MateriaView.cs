using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Application.Views.V1;

public class MateriaView
{
    /// <summary>
    /// The materia slot.
    /// </summary>
    [BsonElement("slotID")]
    [JsonPropertyName("slotID")]
    public int SlotId { get; init; }

    /// <summary>
    /// The materia item ID.
    /// </summary>
    [BsonElement("materiaID")]
    [JsonPropertyName("materiaID")]
    public int MateriaId { get; init; }
}