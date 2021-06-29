using System;
using System.Collections.Generic;

namespace Universalis.GameData
{
    public static class ChineseServers
    {
        /// <summary>
        /// Converts the provided data center name into its Hanzi form. If the provided data center name
        /// is not a Chinese data center, an <see cref="ArgumentException" /> will be thrown.
        /// </summary>
        /// <exception cref="ArgumentException">The provided data center is not a Chinese data center.</exception>
        /// <param name="dataCenter">The data center name, romanized or already in Hanzi.</param>
        /// <returns></returns>
        public static string DataCenterToHanzi(string dataCenter)
            => dataCenter switch
            {
                "LuXingNiao" => "陆行鸟",
                "MoGuLi" => "莫古力",
                "MaoXiaoPang" => "猫小胖",
                "陆行鸟" => "陆行鸟",
                "莫古力" => "莫古力",
                "猫小胖" => "猫小胖",
                _ => throw new ArgumentException("No mapping for the provided data center was found."),
            };

        internal static IEnumerable<DataCenter> DataCenters()
            => new[]
            {
                new DataCenter {Name = "LuXingNiao"},
                new DataCenter {Name = "MoGuLi"},
                new DataCenter {Name = "MaoXiaoPang"},
                new DataCenter {Name = "陆行鸟"}, // LuXingNiao
                new DataCenter {Name = "莫古力"}, // MoGuLi
                new DataCenter {Name = "猫小胖"}, // MaoXiaoPang
            };

        internal static IEnumerable<World> Worlds()
            => new[]
            {
                new World{Name = "红玉海", Id = 1167},
                new World{Name = "神意之地", Id = 1081},
                new World{Name = "拉诺西亚", Id = 1042},
                new World{Name = "幻影群岛", Id = 1044},
                new World{Name = "萌芽池", Id = 1060},
                new World{Name = "宇宙和音", Id = 1173},
                new World{Name = "沃仙曦染", Id = 1174},
                new World{Name = "晨曦王座", Id = 1175},
                new World{Name = "白银乡", Id = 1172},
                new World{Name = "白金幻象", Id = 1076},
                new World{Name = "神拳痕", Id = 1171},
                new World{Name = "潮风亭", Id = 1170},
                new World{Name = "旅人栈桥", Id = 1113},
                new World{Name = "拂晓之间", Id = 1121},
                new World{Name = "龙巢神殿", Id = 1166},
                new World{Name = "梦羽宝境", Id = 1176},
                new World{Name = "紫水栈桥", Id = 1043},
                new World{Name = "延夏", Id = 1169},
                new World{Name = "静语庄园", Id = 1106},
                new World{Name = "摩杜纳", Id = 1045},
                new World{Name = "海猫茶屋", Id = 1177},
                new World{Name = "柔风海湾", Id = 1178},
                new World{Name = "琥珀原", Id = 1179},
            };
    }
}
