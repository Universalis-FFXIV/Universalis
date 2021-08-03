using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class SourceUploadCountView
    {
        /// <summary>
        /// The name of the client application.
        /// </summary>
        [JsonProperty("sourceName")]
        public string Name { get; set; }

        /// <summary>
        /// The number of uploads originating from the client application.
        /// </summary>
        [JsonProperty("uploadCount")]
        public double UploadCount { get; set; }
    }
}