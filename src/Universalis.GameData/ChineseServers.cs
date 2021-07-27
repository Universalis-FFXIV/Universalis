using System.Collections.Generic;

namespace Universalis.GameData
{
    public static class ChineseServers
    {
        /// <summary>
        /// Converts the provided romanized data center or world name into its Hanzi form.
        /// </summary>
        /// <param name="worldOrDc">The romanized name of the world or data center.</param>
        /// <returns>The Hanzi form of the name, or the input data if it is already in Hanzi or no mapping exists.</returns>
        public static string RomanizedToHanzi(string worldOrDc)
            => worldOrDc switch
            {
                "LuXingNiao" => "陆行鸟",
                "MoGuLi" => "莫古力",
                "MaoXiaoPang" => "猫小胖",
                "HongYuHai" => "红玉海",
                "ShenYiZhiDi" => "神意之地",
                "LaNuoXiYa" => "拉诺西亚",
                "HuanYingQunDao" => "幻影群岛",
                "MengYaChi" => "萌芽池",
                "YuZhouHeYin" => "宇宙和音",
                "WoXianXiRan" => "沃仙曦染",
                "ChenXiWangZuo" => "晨曦王座",
                "BaiYinXiang" => "白银乡",
                "BaiJinHuanXiang" => "白金幻象",
                "ShenQuanHen" => "神拳痕",
                "ChaoFengTing" => "潮风亭",
                "LvRenZhanQiao" => "旅人栈桥",
                "FuXiaoZhiJian" => "拂晓之间",
                "Longchaoshendian" => "龙巢神殿",
                "MengYuBaoJing" => "梦羽宝境",
                "ZiShuiZhanQiao" => "紫水栈桥",
                "YanXia" => "延夏",
                "JingYuZhuangYuan" => "静语庄园",
                "MoDuNa" => "摩杜纳",
                "HaiMaoChaWu" => "海猫茶屋",
                "RouFengHaiWan" => "柔风海湾",
                "HuPoYuan" => "琥珀原",
                _ => worldOrDc,
            };

        /// <summary>
        /// Converts the provided Hanzi world or data center name into its romanized form.
        /// </summary>
        /// <param name="worldOrDc">The Hanzi name of the world or data center.</param>
        /// <returns>The romanized form of the name, or the input data if it is already romanized or no mapping exists.</returns>
        public static string HanziToRomanized(string worldOrDc)
            => worldOrDc switch
            {
                "陆行鸟" => "LuXingNiao",
                "莫古力" => "MoGuLi",
                "猫小胖" => "MaoXiaoPang",
                "红玉海" => "HongYuHai",
                "神意之地" => "ShenYiZhiDi",
                "拉诺西亚" => "LaNuoXiYa",
                "幻影群岛" => "HuanYingQunDao",
                "萌芽池" => "MengYaChi",
                "宇宙和音" => "YuZhouHeYin",
                "沃仙曦染" => "WoXianXiRan",
                "晨曦王座" => "ChenXiWangZuo",
                "白银乡" => "BaiYinXiang",
                "白金幻象" => "BaiJinHuanXiang",
                "神拳痕" => "ShenQuanHen",
                "潮风亭" => "ChaoFengTing",
                "旅人栈桥" => "LvRenZhanQiao",
                "拂晓之间" => "FuXiaoZhiJian",
                "龙巢神殿" => "Longchaoshendian",
                "梦羽宝境" => "MengYuBaoJing",
                "紫水栈桥" => "ZiShuiZhanQiao",
                "延夏" => "YanXia",
                "静语庄园" => "JingYuZhuangYuan",
                "摩杜纳" => "MoDuNa",
                "海猫茶屋" => "HaiMaoChaWu",
                "柔风海湾" => "RouFengHaiWan",
                "琥珀原" => "HuPoYuan",
                _ => worldOrDc,
            };

        internal static IEnumerable<DataCenter> DataCenters()
            => new[]
            {
                new DataCenter {Name = "陆行鸟", WorldIds = new uint[]{ 1167, 1081, 1042, 1044, 1060, 1173, 1174, 1175 }},
                new DataCenter {Name = "莫古力", WorldIds = new uint[]{ 1172, 1076, 1171, 1170, 1113, 1121, 1166, 1176 }},
                new DataCenter {Name = "猫小胖", WorldIds = new uint[]{ 1043, 1169, 1106, 1045, 1177, 1178, 1179 }},
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
