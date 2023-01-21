using System.Collections.Generic;

namespace Universalis.GameData;

public static class ChineseServers
{
    // TODO: This should be structured in a less redundant way.

    /// <summary>
    /// Converts the provided romanized data center or world name into its Hanzi form.
    /// </summary>
    /// <param name="worldOrDc">The romanized name of the world or data center.</param>
    /// <returns>The Hanzi form of the name, or the input data if it is already in Hanzi or no mapping exists.</returns>
    public static string RomanizedToHanzi(string worldOrDc)
        => worldOrDc.ToLowerInvariant() switch
        {
            "luxingniao" => "陆行鸟",
            "moguli" => "莫古力",
            "maoxiaopang" => "猫小胖",
            "doudouchai" => "豆豆柴",
            "hongyuhai" => "红玉海",
            "shenyizhidi" => "神意之地",
            "lanuoxiya" => "拉诺西亚",
            "huanyingqundao" => "幻影群岛",
            "mengyachi" => "萌芽池",
            "yuzhouheyin" => "宇宙和音",
            "woxianxiran" => "沃仙曦染",
            "chenxiwangzuo" => "晨曦王座",
            "baiyinxiang" => "白银乡",
            "baijinhuanxiang" => "白金幻象",
            "shenquanhen" => "神拳痕",
            "chaofengting" => "潮风亭",
            "lvrenzhanqiao" => "旅人栈桥",
            "fuxiaozhijian" => "拂晓之间",
            "longchaoshendian" => "龙巢神殿",
            "mengyubaojing" => "梦羽宝境",
            "zishuizhanqiao" => "紫水栈桥",
            "yanxia" => "延夏",
            "jingyuzhuangyuan" => "静语庄园",
            "moduna" => "摩杜纳",
            "haimaochawu" => "海猫茶屋",
            "roufenghaiwan" => "柔风海湾",
            "hupoyuan" => "琥珀原",
            "shuijingta" or "shuijingta2" => "水晶塔",
            "yinleihu" or "yinleihu2" => "银泪湖",
            "taiyanghaian" or "taiyanghaian2" => "太阳海岸",
            "yixiujiade" or "yixiujiade2" => "伊修加德",
            "hongchachuan" or "hongchachuan2" => "红茶川",
            "huangjingu" => "黄金谷",
            "yueyawan" => "月牙湾",
            "xuesongyuan" or "xuesongyuan2" => "雪松原",
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
            "豆豆柴" => "DouDouChai",
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
            "水晶塔" => "ShuiJingTa",
            "银泪湖" => "YinLeiHu",
            "太阳海岸" => "TaiYangHaiAn",
            "伊修加德" => "YiXiuJiaDe",
            "红茶川" => "HongChaChuan",
            "黄金谷" => "HuangJinGu",
            "月牙湾" => "YueYaWan",
            "雪松原" => "XueSongYuan",
            _ => worldOrDc,
        };

    public static string RegionToHanzi(string input)
    {
        return input.ToLowerInvariant() == "china" ? "中国" : input;
    }

    internal static IEnumerable<DataCenter> DataCenters()
        => new[]
        {
            new DataCenter
            {
                Name = "陆行鸟", Region = "中国", WorldIds = new[] { 1167, 1081, 1042, 1044, 1060, 1173, 1174, 1175 },
            },
            new DataCenter
            {
                Name = "莫古力", Region = "中国", WorldIds = new[] { 1172, 1076, 1171, 1170, 1113, 1121, 1166, 1176 },
            },
            new DataCenter
                { Name = "猫小胖", Region = "中国", WorldIds = new[] { 1043, 1169, 1106, 1045, 1177, 1178, 1179 } },
            new DataCenter
            {
                Name = "豆豆柴", Region = "中国", WorldIds = new[] { 1192, 1183, 1180, 1186, 1201, 1068, 1064, 1187 },
            },
        };

    internal static IEnumerable<World> Worlds()
        => new[]
        {
            new World { Name = "红玉海", Id = 1167 },
            new World { Name = "神意之地", Id = 1081 },
            new World { Name = "拉诺西亚", Id = 1042 },
            new World { Name = "幻影群岛", Id = 1044 },
            new World { Name = "萌芽池", Id = 1060 },
            new World { Name = "宇宙和音", Id = 1173 },
            new World { Name = "沃仙曦染", Id = 1174 },
            new World { Name = "晨曦王座", Id = 1175 },
            new World { Name = "白银乡", Id = 1172 },
            new World { Name = "白金幻象", Id = 1076 },
            new World { Name = "神拳痕", Id = 1171 },
            new World { Name = "潮风亭", Id = 1170 },
            new World { Name = "旅人栈桥", Id = 1113 },
            new World { Name = "拂晓之间", Id = 1121 },
            new World { Name = "龙巢神殿", Id = 1166 },
            new World { Name = "梦羽宝境", Id = 1176 },
            new World { Name = "紫水栈桥", Id = 1043 },
            new World { Name = "延夏", Id = 1169 },
            new World { Name = "静语庄园", Id = 1106 },
            new World { Name = "摩杜纳", Id = 1045 },
            new World { Name = "海猫茶屋", Id = 1177 },
            new World { Name = "柔风海湾", Id = 1178 },
            new World { Name = "琥珀原", Id = 1179 },
            new World { Name = "水晶塔", Id = 1192 },
            new World { Name = "银泪湖", Id = 1183 },
            new World { Name = "太阳海岸", Id = 1180 },
            new World { Name = "伊修加德", Id = 1186 },
            new World { Name = "红茶川", Id = 1201 },
            new World { Name = "黄金谷", Id = 1068 },
            new World { Name = "月牙湾", Id = 1064 },
            new World { Name = "雪松原", Id = 1187 },
        };
}