using System.Collections.Generic;

namespace Universalis.GameData;

public static class Regions
{
    public static readonly IReadOnlyDictionary<byte, string> Map = new Dictionary<byte, string>
    {
        { 1, "Japan" },
        { 2, "North-America" },
        { 3, "Europe" },
        { 4, "Oceania" },
        { 5, "China" },
        { 6, "Eorzea" }, // ?
        { 7, "NA-Cloud-DC" }, // NA Cloud DC (Beta)
    };
}