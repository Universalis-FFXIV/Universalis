using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views
{
    public class MostRecentlyUpdatedItemsView
    {
        /// <summary>
        /// A list of item upload information in timestamp-descending order.
        /// </summary>
        [JsonPropertyName("items")]
        public List<WorldItemRecencyView> Items { get; init; } = new();
    }
}