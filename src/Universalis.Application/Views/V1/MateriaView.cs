using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1;

public class MateriaView
{
    /// <summary>
    /// The materia slot.
    /// </summary>
    [JsonPropertyName("slotID")]
    public uint SlotId { get; init; }

    /// <summary>
    /// The materia item ID.
    /// </summary>
    [JsonPropertyName("materiaID")]
    public uint MateriaId { get; init; }
}