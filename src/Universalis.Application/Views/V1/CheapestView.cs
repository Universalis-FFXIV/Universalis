using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V1;

public class CheapestView
{
    [JsonPropertyName("items")]
    public SortedDictionary<int, ListingView> Items { get; init; }
}