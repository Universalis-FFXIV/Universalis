using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class SourceUploadCountView
    {
        [JsonProperty("sourceName")]
        public string Name { get; set; }

        [JsonProperty("uploadCount")]
        public double UploadCount { get; set; }
    }
}