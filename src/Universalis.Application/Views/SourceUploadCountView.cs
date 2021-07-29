using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class SourceUploadCountView
    {
        [JsonProperty("sourceName")]
        public string Name { get; set; }

        [JsonProperty("uploadCount")]
        public uint UploadCount { get; set; }
    }
}