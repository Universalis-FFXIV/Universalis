using System.Collections.Generic;
using Newtonsoft.Json;

namespace Universalis.Application.Views
{
    public class MostRecentlyUpdatedItemsView
    {
        [JsonProperty("items")]
        public List<WorldItemRecencyView> Items { get; set; } = new();
    }
}