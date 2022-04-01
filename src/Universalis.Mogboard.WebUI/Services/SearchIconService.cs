namespace Universalis.Mogboard.WebUI.Services;

public class SearchIconService : ISearchIconService
{
    public string GetSearchIcon(int id)
    {
        return id switch
        {
            10 => "class_job_001",
            11 => "class_job_003",
            76 => "class_job_032",
            13 => "class_job_004",
            9 => "class_job_002",
            83 => "class_job_034",
            73 => "class_job_029",
            12 => "class_job_005",
            77 => "class_job_031",
            14 => "class_job_007",
            16 => "class_job_026",
            84 => "class_job_035",
            15 => "class_job_006",
            85 => "class_job_028",
            78 => "class_job_033",
            19 => "class_job_008",
            20 => "class_job_009",
            21 => "class_job_010",
            22 => "class_job_011",
            23 => "class_job_012",
            24 => "class_job_013",
            25 => "class_job_014",
            26 => "class_job_015",
            27 => "class_job_016",
            28 => "class_job_017",
            29 => "class_job_018",
            30 => "ItemCategory_Fishing_Tackle",

            17 => "ItemCategory_Shield",
            31 => "Armoury_Head",
            33 => "Armoury_Body",
            36 => "Armoury_Hands",
            38 => "Armoury_Waist",
            35 => "Armoury_Legs",
            37 => "Armoury_Feet",
            40 => "Armoury_Earrings",
            39 => "Armoury_Necklace",
            41 => "Armoury_Bracelets",
            42 => "Armoury_Ring",

            43 => "ItemCategory_Medicine",
            44 => "ItemCategory_CUL",
            45 => "ItemCategory_Meal",
            46 => "ItemCategory_fisher",
            47 => "ItemCategory_MIN",
            48 => "ItemCategory_ARM",
            49 => "ItemCategory_CRP",
            50 => "ItemCategory_WVR",
            51 => "ItemCategory_LTW",
            52 => "ItemCategory_Bone",
            53 => "ItemCategory_ALC",
            54 => "ItemCategory_Dye",
            55 => "ItemCategory_Part",
            57 => "ItemCategory_Materia",
            58 => "ItemCategory_Crystal",
            59 => "ItemCategory_Catalyst",
            60 => "ItemCategory_Miscellany",
            74 => "ItemCategory_Seasonal_Miscellany",
            75 => "ItemCategory_Minion",
            79 => "ItemCategory_Airship",
            80 => "ItemCategory_Orchestrion_Roll",

            65 => "ItemCategory_Exterior_Fixtures",
            66 => "ItemCategory_Interior_Fixtures",
            67 => "ItemCategory_Outdoor_Furnishings",
            56 => "ItemCategory_Furnishings",
            68 => "ItemCategory_Chairs_and_Beds",
            69 => "ItemCategory_Tables",
            70 => "ItemCategory_Tabletop",
            71 => "ItemCategory_Wallmounted",
            72 => "ItemCategory_Rug",
            81 => "ItemCategory_Gardening",
            82 => "ItemCategory_Painting",

            86 => "class_job_037", // Gunbreaker"s Arms
            87 => "class_job_038", // Dancer"s Arms
            88 => "class_job_039", // Reaper"s Arms
            89 => "class_job_040", // Sage"s Arms

            _ => "",
        };
    }
}