using System.Text.Json.Serialization;

namespace Universalis.Application.Uploads.Schema
{
    public class Materia
    {
        [JsonPropertyName("slotID")]
        public uint SlotId { get; set; }
        
        [JsonPropertyName("materiaID")]
        public uint MateriaId { get; set; }
    }
}