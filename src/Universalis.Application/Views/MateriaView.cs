using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class MateriaView
    {
        /// <summary>
        /// The materia slot.
        /// </summary>
        [JsonProperty("slotID")]
        public uint SlotId { get; set; }

        /// <summary>
        /// The materia item ID.
        /// </summary>
        [JsonProperty("materiaID")]
        public uint MateriaId { get; set; }
    }
}