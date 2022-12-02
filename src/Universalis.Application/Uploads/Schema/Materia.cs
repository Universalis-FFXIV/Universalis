using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema;

public class Materia
{
    [JsonPropertyName("slotID")]
    public int? SlotId { get; set; }
        
    [JsonPropertyName("materiaID")]
    public int? MateriaId { get; set; }
}