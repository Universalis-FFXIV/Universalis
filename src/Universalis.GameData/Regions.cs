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
        { 7, "North-America" }, // NA Cloud DC
    };
}