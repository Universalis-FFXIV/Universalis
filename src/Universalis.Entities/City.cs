using System.Collections.Generic;

namespace Universalis.Entities;

internal static class City
{
    public static readonly Dictionary<string, byte> Dict = new()
    {
        { "Nowheresville", 0 },
        { "Limsa Lominsa", 1 },
        { "Gridania", 2 },
        { "Ul'dah", 3 },
        { "Ishgard", 4 },
        { "Kugane", 7 },
        { "Crystarium", 10 },
    };
}