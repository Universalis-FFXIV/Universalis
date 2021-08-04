using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class RecentlyUpdatedItemsView
    {
        /// <summary>
        /// A list of item IDs, with the most recent first.
        /// </summary>
        [JsonProperty("items")]
        public List<uint> Items { get; set; } = new();
    }
}