using Newtonsoft.Json;
using System.Collections.Generic;

namespace Universalis.Application.Views
{
    public class UploadCountHistoryView
    {
        /// <summary>
        /// The list of upload counts per day, over the past 30 days.
        /// </summary>
        [JsonProperty("uploadCountByDay")]
        public IList<double> UploadCountByDay { get; set; }
    }
}