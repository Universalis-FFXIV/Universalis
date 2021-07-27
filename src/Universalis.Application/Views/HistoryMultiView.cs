using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class HistoryMultiView
    {
        [JsonProperty("itemIDs")]
        public uint[] ItemIds { get; set; }

        [JsonProperty("items")]
        public List<HistoryView> Items { get; set; }

        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        [JsonProperty("unresolvedItems")]
        public uint[] UnresolvedItemIds { get; set; }
    }
}