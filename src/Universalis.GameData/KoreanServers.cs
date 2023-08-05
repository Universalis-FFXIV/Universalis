using System.Collections.Generic;

namespace Universalis.GameData;

public static class KoreanServers
{
    /// <summary>
    /// Converts the provided romanized data center or world name into its Hangul form.
    /// </summary>
    /// <param name="worldOrDc">The romanized name of the world or data center.</param>
    /// <returns>The Hangul form of the name, or the input data if it is already in Hangul or no mapping exists.</returns>
    public static string RomanizedToHangul(string worldOrDc)
        => worldOrDc.ToLowerInvariant() switch
        {
            "KrCarbuncle" => "카벙클",
            "KrChocobo" => "초코보",
            "KrMoogle" => "모그리",
            "KrTonberry" => "톤베리",
            "KrFenrir" => "펜리르",
            _ => worldOrDc,
        };

    /// <summary>
    /// Converts the provided Hangul world or data center name into its romanized form.
    /// </summary>
    /// <param name="worldOrDc">The Hangul name of the world or data center.</param>
    /// <returns>The romanized form of the name, or the input data if it is already romanized or no mapping exists.</returns>
    public static string HangulToRomanized(string worldOrDc)
        => worldOrDc switch
        {
            "카벙클" => "KrCarbuncle",
            "초코보" => "KrChocobo",
            "모그리" => "KrMoogle",
            "톤베리" => "KrTonberry",
            "펜리르" => "KrFenrir",
            _ => worldOrDc,
        };

    public static string RegionToHangul(string input)
    {
        return input.ToLowerInvariant() == "korea" ? "한국" : input;
    }

    internal static IEnumerable<DataCenter> DataCenters()
        => new[]
        {
            new DataCenter
            {
                Name = "한국", Region = "한국", WorldIds = new[] {2075, 2076, 2077, 2078, 2080}
            }
        };

    internal static IEnumerable<World> Worlds()
        => new[]
        {
            new World {Name = "카벙클", Id = 2075},
            new World {Name = "초코보", Id = 2076},
            new World {Name = "모그리", Id = 2077},
            new World {Name = "톤베리", Id = 2078},
            new World {Name = "펜리르", Id = 2080}
        };
}