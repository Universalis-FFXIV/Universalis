using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class MateriaView
    {
        [JsonProperty("slotID")]
        public uint SlotId { get; set; }

        [JsonProperty("materiaID")]
        public uint MateriaId { get; set; }
    }
}