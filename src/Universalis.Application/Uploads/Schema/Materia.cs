using Newtonsoft.Json;

namespace Universalis.Application.Uploads.Schema
{
    public class Materia
    {
        [JsonProperty("slotID")]
        public uint SlotId { get; set; }
        
        [JsonProperty("materiaID")]
        public uint MateriaId { get; set; }
    }
}