using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class ContentView
    {
        [JsonProperty("contentID")]
        public string ContentId { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("characterName", NullValueHandling = NullValueHandling.Ignore)]
        public string CharacterName { get; set; }
    }
}