using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class MostRecentlyUpdatedItemsView
    {
        /// <summary>
        /// A list of item upload information in timestamp-descending order.
        /// </summary>
        [JsonProperty("items")]
        public List<WorldItemRecencyView> Items { get; set; } = new();
    }
}