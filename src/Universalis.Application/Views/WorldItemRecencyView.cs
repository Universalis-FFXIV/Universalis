using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class WorldItemRecencyView
    {
        /// <summary>
        /// The item ID.
        /// </summary>
        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        /// <summary>
        /// The last upload time for the item on the listed world.
        /// </summary>
        [JsonProperty("lastUploadTime")]
        public double LastUploadTimeUnixMilliseconds { get; set; }

        /// <summary>
        /// The world ID.
        /// </summary>
        [JsonProperty("worldID")]
        public uint WorldId { get; set; }

        /// <summary>
        /// The world name.
        /// </summary>
        [JsonProperty("worldName")]
        public string WorldName { get; set; }
    }
}