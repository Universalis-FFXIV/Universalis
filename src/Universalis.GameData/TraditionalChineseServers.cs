using System.Collections.Generic;

namespace Universalis.GameData;

public static class TraditionalChineseServers
{
    /// <summary>
    /// Converts the provided romanized data center or world name into its Traditional Chinese form.
    /// </summary>
    /// <param name="worldOrDc">The romanized name of the world or data center.</param>
    /// <returns>The Traditional Chinese form of the name, or the input data if it is already in Traditional Chinese or no mapping exists.</returns>
    public static string RomanizedToTraditionalChinese(string worldOrDc)
        => worldOrDc.ToLowerInvariant() switch
        {
            "tcifrit" => "伊弗利特",
            "tcgaruda" => "迦樓羅",
            "tcleviathan" => "利維坦",
            "tcphoenix" => "鳳凰",
            "tcodin" => "奧汀",
            "tcbahamut" => "巴哈姆特",
            "tcramuh" => "拉姆",
            "tctitan" => "泰坦",
            "tcluxingniao" => "陸行鳥",
            _ => worldOrDc,
        };

    /// <summary>
    /// Converts the provided Traditional Chinese world or data center name into its romanized form.
    /// </summary>
    /// <param name="worldOrDc">The Traditional Chinese name of the world or data center.</param>
    /// <returns>The romanized form of the name, or the input data if it is already romanized or no mapping exists.</returns>
    public static string TraditionalChineseToRomanized(string worldOrDc)
        => worldOrDc switch
        {
            "伊弗利特" => "TcIfrit",
            "迦樓羅" => "TcGaruda",
            "利維坦" => "TcLeviathan",
            "鳳凰" => "TcPhoenix",
            "奧汀" => "TcOdin",
            "巴哈姆特" => "TcBahamut",
            "拉姆" => "TcRamuh",
            "泰坦" => "TcTitan",
            "陸行鳥" => "TcLuXingNiao",
            _ => worldOrDc,
        };

    public static string RegionToTraditionalChinese(string input)
    {
        return input.ToLowerInvariant() == "traditionalchinese" ? "繁中服" : input;
    }

    internal static IEnumerable<DataCenter> DataCenters()
        => new[]
        {
            new DataCenter
            {
                Name = "陸行鳥",
                Region = "繁中服",
                WorldIds = new[] { 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035 }
            }
        };

    internal static IEnumerable<World> Worlds()
        => new[]
        {
            new World { Name = "伊弗利特", Id = 4028 },
            new World { Name = "迦樓羅", Id = 4029 },
            new World { Name = "利維坦", Id = 4030 },
            new World { Name = "鳳凰", Id = 4031 },
            new World { Name = "奧汀", Id = 4032 },
            new World { Name = "巴哈姆特", Id = 4033 },
            new World { Name = "拉姆", Id = 4034 },
            new World { Name = "泰坦", Id = 4035 },
        };
}
