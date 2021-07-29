using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universalis.Application.Views
{
    public class UploadCountHistoryView
    {
        [JsonProperty("uploadCountByDay")]
        public IList<uint> UploadCountByDay { get; set; }
    }
}