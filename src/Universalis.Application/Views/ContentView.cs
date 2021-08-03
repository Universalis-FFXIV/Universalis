using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class ContentView
    {
        /// <summary>
        /// The content ID of the object.
        /// </summary>
        [JsonProperty("contentID")]
        public string ContentId { get; set; }

        /// <summary>
        /// The content type of this object.
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// The character name associated with this character object, if this is one.
        /// </summary>
        [JsonProperty("characterName", NullValueHandling = NullValueHandling.Ignore)]
        public string CharacterName { get; set; }
    }
}