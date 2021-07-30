using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class CurrentlyShownMultiView
    {
        [JsonProperty("itemIDs")]
        public List<uint> ItemIds { get; set; } = new();

        [JsonProperty("items")]
        public List<CurrentlyShownView> Items { get; set; } = new();

        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        [JsonProperty("unresolvedItems")]
        public uint[] UnresolvedItemIds { get; set; }
    }

    public class CurrentlyShownMultiViewV2
    {
        [JsonProperty("itemIDs")]
        public uint[] ItemIds { get; set; }

        [JsonProperty("items")]
        public Dictionary<uint, CurrentlyShownView> Items { get; set; } = new();

        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        [JsonProperty("unresolvedItems")]
        public uint[] UnresolvedItemIds { get; set; }
    }
}