using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class CurrentlyShownMultiView
    {
        /// <summary>
        /// The item IDs that were requested.
        /// </summary>
        [JsonProperty("itemIDs")]
        public List<uint> ItemIds { get; set; } = new();

        /// <summary>
        /// The item data that was requested, as a list. Use the nested item IDs
        /// to pull the item you want, or consider using the v2 endpoint instead.
        /// </summary>
        [JsonProperty("items")]
        public List<CurrentlyShownView> Items { get; set; } = new();

        /// <summary>
        /// The ID of the world requested, if applicable.
        /// </summary>
        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        /// <summary>
        /// The name of the world requested, if applicable.
        /// </summary>
        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }

        /// <summary>
        /// The name of the DC requested, if applicable.
        /// </summary>
        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        /// <summary>
        /// A list of IDs that could not be resolved to any item data.
        /// </summary>
        [JsonProperty("unresolvedItems")]
        public uint[] UnresolvedItemIds { get; set; }
    }

    public class CurrentlyShownMultiViewV2
    {
        /// <summary>
        /// The item IDs that were requested.
        /// </summary>
        [JsonProperty("itemIDs")]
        public List<uint> ItemIds { get; set; } = new();

        /// <summary>
        /// The item data that was requested, keyed on the item ID.
        /// </summary>
        [JsonProperty("items")]
        public Dictionary<uint, CurrentlyShownView> Items { get; set; } = new();

        /// <summary>
        /// The ID of the world requested, if applicable.
        /// </summary>
        [JsonProperty("worldID", NullValueHandling = NullValueHandling.Ignore)]
        public uint? WorldId { get; set; }

        /// <summary>
        /// The name of the world requested, if applicable.
        /// </summary>
        [JsonProperty("worldName", NullValueHandling = NullValueHandling.Ignore)]
        public string WorldName { get; set; }

        /// <summary>
        /// The name of the DC requested, if applicable.
        /// </summary>
        [JsonProperty("dcName", NullValueHandling = NullValueHandling.Ignore)]
        public string DcName { get; set; }

        /// <summary>
        /// A list of IDs that could not be resolved to any item data.
        /// </summary>
        [JsonProperty("unresolvedItems")]
        public uint[] UnresolvedItemIds { get; set; }
    }
}