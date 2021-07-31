using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class WorldItemRecencyView
    {
        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        [JsonProperty("lastUploadTime")]
        public long LastUploadTimeUnixMilliseconds { get; set; }

        [JsonProperty("worldID")]
        public uint WorldId { get; set; }

        [JsonProperty("worldName")]
        public string WorldName { get; set; }
    }
}