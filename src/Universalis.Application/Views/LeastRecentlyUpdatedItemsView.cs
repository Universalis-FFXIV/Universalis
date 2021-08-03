using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class LeastRecentlyUpdatedItemsView
    {
        /// <summary>
        /// A list of item upload information in timestamp-ascending order.
        /// </summary>
        [JsonProperty("items")]
        public List<WorldItemRecencyView> Items { get; set; } = new();
    }
}